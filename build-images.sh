#!/bin/bash

docker build -t local-exchange-broadcaster ExchangeBroadcaster --no-cache && \
docker build -t local-queue-broadcaster QueueBroadcaster --no-cache && \
docker build -t local-exchange-receiver ExchangeReceiver --no-cache && \
docker build -t local-queue-receiver QueueReceiver --no-cache && \
echo "Done"
