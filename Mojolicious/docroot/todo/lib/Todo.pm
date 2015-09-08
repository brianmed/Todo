package Todo;

use Mojo::Base 'Mojolicious';

use SiteCode::Account;
use SiteCode::DBX;

sub site_dir
{
    state $site_dir = pop;
}

sub site_config
{
    state $site_config = pop;
}

# This method will run once at server start
sub startup {
    my $self = shift;

    $self->log->level("debug");

    my $site_config = $self->plugin("Config" => {file => '/opt/todo.config'});
    $self->secrets([$$site_config{site_secret}]);

    $self->helper(site_dir => \&site_dir);
    $self->helper(site_config => \&site_config);
    $self->site_dir($$site_config{site_dir});
    $self->site_config($site_config);

    my $listen = [];
    push(@{ $listen }, "http://$$site_config{hypnotoad_ip}:$$site_config{hypnotoad_port}") if $$site_config{hypnotoad_port};
    push(@{ $listen }, "https://$$site_config{hypnotoad_ip}:$$site_config{hypnotoad_tls}") if $$site_config{hypnotoad_tls};

    $self->config(hypnotoad => {listen => $listen, workers => $$site_config{hypnotoad_workers}, user => $$site_config{user}, group => $$site_config{group}, inactivity_timeout => 15, heartbeat_timeout => 15, heartbeat_interval => 15, accepts => 100});

    $self->plugin(AccessLog => {uname_helper => 'set_username', log => "$$site_config{site_dir}/docroot/todo/log/access.log", format => '%h %l %u %t "%r" %>s %b %D "%{Referer}i" "%{User-Agent}i"'});
    
    # Router
    my $r = $self->routes;

    my $api = $r->under (sub {
        my $self = shift;

        return($self->render(json => {status => "error", data => { message => "No JSON found" }})) unless $self->req->json;

        my $site_dir = $self->site_dir;
        my $username = $self->req->json->{username};
        my $api_key = $self->req->json->{api_key};

        unless ($username) {
            $self->render(json => {status => "error", data => { message => "No username found" }});

            return undef;
        }

        my $account = SiteCode::Account->new(username => $username);

        unless ($account->{id}) {
            $self->render(json => {status => "error", data => { message => "No account found" }});

            return undef;
        }

        unless ($api_key) {
            $self->render(json => {status => "error", data => { message => "No API Key found" }});

            return undef;
        }

        unless ($api_key eq $account->key("api_key")) {
            $self->render(json => {status => "error", data => { message => "Credentials mis-match" }});

            return undef;
        }

        $self->set_username($account->{username});

        return 1;
    });

    $r->get('/api/v1/account/register')->to(controller => "API", action => "post");
    $r->post('/api/v1/account/register')->to(controller => "API", action => "register");

    $r->get('/api/v1/account/signin')->to(controller => "API", action => "post");
    $r->post('/api/v1/account/signin')->to(controller => "API", action => "signin");

    $r->get('/api/v1/account/get_todos')->to(controller => "API", action => "post");
    $r->post('/api/v1/account/get_todos')->to(controller => "API", action => "get_todos");

    $r->get('/api/v1/account/del_todo')->to(controller => "API", action => "post");
    $r->post('/api/v1/account/del_todo')->to(controller => "API", action => "del_todo");

    $r->get('/api/v1/account/add_todo')->to(controller => "API", action => "post");
    $r->post('/api/v1/account/add_todo')->to(controller => "API", action => "add_todo");
}

1;
