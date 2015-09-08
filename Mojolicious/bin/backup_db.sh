#!/bin/bash

set -e

cd /opt/todo/sql/backup

FILE=todo.txt.$(date "+%FT%T")
sudo -u postgres /usr/pgsql-9.3/bin/pg_dump todo -f $FILE
gzip $FILE

# Keep three days of backups
ls todo.txt.201* | grep -v -f <(ls todo.txt.201* | sort | tail -72) | xargs -r rm
