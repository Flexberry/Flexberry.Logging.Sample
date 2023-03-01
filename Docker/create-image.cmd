docker build --no-cache -f SQL\Dockerfile.PostgreSql -t flexberrysamplelogging/postgre-sql ../SQL

docker build --no-cache -f Dockerfile -t flexberrysamplelogging/app ../..
