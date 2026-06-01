#!/usr/bin/env bash
#
# Wrapper around the Cadence CLI inside the cadence-server container.
#
# The auto-setup image binds the frontend to the container's broadcast IP
# (e.g. 172.19.0.3:7933), not 127.0.0.1, so the CLI's default --address fails
# with "connection refused". This script resolves the container IP at runtime
# and defaults --domain to "default".
#
# Usage:
#   ./cadence-cli.sh workflow list
#   ./cadence-cli.sh workflow show --workflow_id <id>
#   DOMAIN=other ./cadence-cli.sh workflow list   # override the domain
#
set -euo pipefail

CONTAINER="${CADENCE_CONTAINER:-cadence-server}"
DOMAIN="${DOMAIN:-default}"

docker exec "$CONTAINER" sh -c \
  'd=$1; shift; cadence --address $(hostname -i):7933 --domain "$d" "$@"' _ "$DOMAIN" "$@"
