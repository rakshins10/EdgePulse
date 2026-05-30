# ── Build stage ─────────────────────────────────────────────────────────────
FROM node:20-alpine AS build

WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY tsconfig.json ./
COPY src/ ./src/
RUN npm run build

# ── Runtime stage ────────────────────────────────────────────────────────────
FROM node:20-alpine AS runtime

WORKDIR /app
COPY package*.json ./
RUN npm ci --omit=dev

COPY --from=build /app/dist ./dist

# Runtime expects: PORT, EDGEPULSE_API_URL, SERVICE_KEY, RABBITMQ_URL
# Pass via environment variables or an .env file mounted at runtime.
EXPOSE 3000

CMD ["node", "dist/main.js"]
