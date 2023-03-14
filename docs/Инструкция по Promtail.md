# Инструкция по Promtail

## Что такое Promtail

`Promtail` — это агент, разработанный специально для Loki. Promtail отвечает за сбор логов и отправку их в Loki. Логи могут собираться из файлов, журналов syslog, object store-ов, поддерживает бакеты s3.

## Dockerfile

Существует официальный докер образ `Promtail`-а. Пример докерфайла:

    FROM grafana/promtail:2.7.4

    COPY Dockerfiles/promtail-config.yaml /etc/promtail/promtail-config.yaml

## Файл настроек promtail-config.yaml

Файл настроек конфигурации `promtail-config.yaml` может выглядеть следующим образом:

    server:
      http_listen_port: 9080
      grpc_listen_port: 0

    positions:
      filename: /tmp/positions.yaml

    clients:
      - url: http://loki:3100/loki/api/v1/push

    scrape_configs:
    - job_name: system
      static_configs:
      - targets:
          - localhost
        labels:
          job: varlogs
          __path__: /var/log/*log

* В разделе `server` прописываем порты "прослушки".
* Блок `positions` определяет как сохранять promtail файлы
* `clients` описывает, как Promtail должен подключаться к нескольким экземплярам Grafana Loki и отправлять каждому логи (однако рекомендуется запускать несколько клиентов Promtail параллельно, если вы хотите отправлять данные на несколько удаленных экземпляров Loki.).
* Блок `scrape_configs` настраивает, как Promtail может фильтровать логи.

В примере блок `scrape_configs` представлен в простейшем виде. В данном случае всем логам, которые будут сохранены по пути /var/log/*log, будет присвоен лейбл `job` со значением `varlogs`.

Подробнее про лейблы и их настройку см. в статье "Работа с Grafana Loki" в разделе "Структура логов и использование лейблов".

## Docker compose

`docker-compose.yml` может выглядеть следующим образом:

    networks:
      loki:

    services:
      promtail:
        image: flexberry/promtail
        volumes:
          - /var/log:/var/log
        command: -config.file=/etc/promtail/config.yml
        networks:
          - loki

Сервисы Loki и Grafana также используют network loki.
