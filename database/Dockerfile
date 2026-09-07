FROM mcr.microsoft.com/mssql/server:2022-latest

USER root

RUN mkdir -p /docker-init/migrations
COPY SIBI.sql    /docker-init/SIBI.sql
COPY seed.sql    /docker-init/seed.sql
COPY migrations/ /docker-init/migrations/
COPY init.sh     /docker-init/init.sh
RUN chmod +x /docker-init/init.sh

USER mssql

EXPOSE 1433
ENTRYPOINT ["/docker-init/init.sh"]
