# EdgePulse.Dashboard — Vite build served by nginx
#
# Build CONTEXT is this folder:
#   docker build -f src/EdgePulse.Dashboard/Dockerfile -t edgepulse-dashboard src/EdgePulse.Dashboard

# ── Build stage: compile the React app to static files ───────────────────────
FROM node:20-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . ./
RUN npm run build          # tsc -b && vite build  → outputs to /app/dist

# ── Runtime stage: serve the static files with nginx ─────────────────────────
FROM nginx:1.27-alpine AS runtime

# Where the dashboard forwards /api calls. Override at run time if the API is
# reachable at a different address, e.g. -e API_URL=http://my-api:8080
ENV API_URL=http://edgepulse-api:8080

COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx/default.conf.template /etc/nginx/templates/default.conf.template

EXPOSE 80
# The nginx base image's entrypoint runs envsubst on the template, then starts
# nginx in the foreground — no custom CMD needed.
