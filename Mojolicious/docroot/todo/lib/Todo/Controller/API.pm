package Todo::Controller::API;

use Mojo::Base 'Mojolicious::Controller';
use Mojo::UserAgent;
use Data::UUID;
use Mojo::Util qw(md5_sum);
use Mojo::JSON qw(encode_json decode_json);

sub post {
    my $c = shift;

    $c->render(json => {status => "error", data => { message => "Use POST" }});
}

sub register {
    my $c = shift;

    my $username = $c->req->json->{username};
    my $password = $c->req->json->{password};

    # return($c->render(json => {status => "error", data => { message => "We're experiencing growing pains.  Please try again in a few days." }}));

    if (SiteCode::Account->exists(username => $username)) {
        return($c->render(json => {status => "error", data => { message => "Username taken" }}));
    }

    if (8 > length($password)) {
        return($c->render(json => {status => "error", data => { message => "Password needs to be at least 8 characters" }}));
    }
    if ($password !~ m/[0-9]/ && $password !~ m/[A-Z]/ && $password !~ m/[a-z]/) {
        return($c->render(json => {status => "error", data => { message => "Password needs at least one digit, upper case, and lower case character" }}));
    }

    if (3 > length($username)) {
        return($c->render(json => {status => "error", data => { message => "Username needs to be at least 3 characters" }}));
    }
    unless ($username =~ m/^[0-9A-Za-z_]+$/) {
        return($c->render(json => {status => "error", data => { message => "Username needs to be a digit, uppercase, lower case, or an _." }}));
    }

    my $json = $c->tx->req->json;

    my ($id, $account);
    eval {
        my $hash = {
            password => $password,
            username => $username,
        };

        $id = SiteCode::Account->insert($hash);
        my $ug = Data::UUID->new;

        $account = SiteCode::Account->new(username => $username);
        $account->key("api_key", $ug->create_str());
    };
    if ($@) {
        $c->app->log->debug("API:register:$@");
        return($c->render(json => {status => "error", data => { message => "Please contact support" }}));
    }

    return($c->render(json => {status => "success", data => { message => "Successfully registered", api_key => $account->key("api_key") }}));
}

sub signin {
    my $c = shift;

    my $username = $c->req->json->{username};
    my $password = $c->req->json->{password};

    unless (SiteCode::Account->exists(username => $username)) {
        return($c->render(json => {status => "error", data => { message => "Invalid username" }}));
    }

    my $account = SiteCode::Account->new(username => $username);

    unless ($account->check_password($password, $account->{password})) {
        $c->stash(error => "Credentials mis-match");
        return($c->render(json => {status => "error", data => { message => "Credentials mis-match" }}));
    }

    return($c->render(json => {status => "success", data => { message => "Successful login", username => $username, api_key => $account->key("api_key") }}));
}

sub add_todo {
    my $c = shift;

    my $account = SiteCode::Account->new(username => $c->req->json->{username});

    my %json = %{ $c->req->json };
    delete $json{api_key};

    $c->app->log->debug(sprintf("title: %s", $json{title}));

    my $dbx = SiteCode::DBX->new();
    $dbx->do(
        "INSERT INTO todo (account_id, todo) VALUES (?, ?)", undef, 
        $account->id(), encode_json(\%json)
    );
    $dbx->dbh->commit;

    $c->render(json => { status => "success" });
}

sub del_todo {
    my $c = shift;

    my $account = SiteCode::Account->new(username => $c->req->json->{username});

    my %json = %{ $c->req->json };
    delete $json{api_key};

    $c->app->log->debug(sprintf("del_todo: " . $c->req->json->{todo_id}));

    my $dbx = SiteCode::DBX->new();
    $dbx->do(
        "DELETE FROM todo WHERE id = ?", undef, 
        $c->req->json->{todo_id}
    );
    $dbx->dbh->commit;

    $c->render(json => { status => "success" });
}

sub get_todos {
    my $c = shift;

    my $account = SiteCode::Account->new(username => $c->req->json->{username});
    my $dbx = SiteCode::DBX->new();

    my $array = $dbx->array(qq(
        select
            todo::json->>'title' as title,
            todo::json->>'content' as content,
            todo.id as todo_id
        from todo
        where account_id = ?
    ), undef, $account->id());

    $c->render(json => { status => "success", todos => $array });
}

1;
