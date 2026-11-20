# Imagen de producción del frontend (Next.js, salida standalone).
# Render construye esta imagen con el contexto en ./frontend:
#   dockerfilePath: ./frontend/Dockerfile
#   dockerContext:  ./frontend
#
# Las variables NEXT_PUBLIC_* se inlinean en el bundle durante `next build`.
# Render las expone como build args (ARG) al construir la imagen, por eso
# deben existir como variables de entorno del servicio ANTES del deploy.

########################################
# Etapa 1: dependencias
########################################
FROM node:22-alpine AS deps
WORKDIR /app

# Compatibilidad con binarios nativos (sharp/swc) en musl.
RUN apk add --no-cache libc6-compat

COPY package.json package-lock.json ./
RUN npm ci

########################################
# Etapa 2: build
########################################
FROM node:22-alpine AS builder
WORKDIR /app

ARG NEXT_PUBLIC_API_URL
ARG NEXT_PUBLIC_FIREBASE_API_KEY
ARG NEXT_PUBLIC_FIREBASE_AUTH_DOMAIN
ARG NEXT_PUBLIC_FIREBASE_PROJECT_ID
ARG NEXT_PUBLIC_FIREBASE_STORAGE_BUCKET
ARG NEXT_PUBLIC_FIREBASE_MESSAGING_SENDER_ID
ARG NEXT_PUBLIC_FIREBASE_APP_ID
ARG NEXT_PUBLIC_WHATSAPP_NUMBER
ARG NEXT_PUBLIC_CURRENCY

ENV NEXT_PUBLIC_API_URL=$NEXT_PUBLIC_API_URL \
    NEXT_PUBLIC_FIREBASE_API_KEY=$NEXT_PUBLIC_FIREBASE_API_KEY \
    NEXT_PUBLIC_FIREBASE_AUTH_DOMAIN=$NEXT_PUBLIC_FIREBASE_AUTH_DOMAIN \
    NEXT_PUBLIC_FIREBASE_PROJECT_ID=$NEXT_PUBLIC_FIREBASE_PROJECT_ID \
    NEXT_PUBLIC_FIREBASE_STORAGE_BUCKET=$NEXT_PUBLIC_FIREBASE_STORAGE_BUCKET \
    NEXT_PUBLIC_FIREBASE_MESSAGING_SENDER_ID=$NEXT_PUBLIC_FIREBASE_MESSAGING_SENDER_ID \
    NEXT_PUBLIC_FIREBASE_APP_ID=$NEXT_PUBLIC_FIREBASE_APP_ID \
    NEXT_PUBLIC_WHATSAPP_NUMBER=$NEXT_PUBLIC_WHATSAPP_NUMBER \
    NEXT_PUBLIC_CURRENCY=$NEXT_PUBLIC_CURRENCY \
    NEXT_TELEMETRY_DISABLED=1

COPY --from=deps /app/node_modules ./node_modules
COPY . .

RUN npm run build

########################################
# Etapa 3: runtime
########################################
FROM node:22-alpine AS runner
WORKDIR /app

ENV NODE_ENV=production \
    HOSTNAME=0.0.0.0 \
    PORT=${PORT:-10000}

# `output: "standalone"` genera un server.js mínimo que ya incluye las
# dependencias necesarias; public/ y .next/static se copian aparte.
COPY --from=builder --chown=node:node /app/public ./public
COPY --from=builder --chown=node:node /app/.next/standalone ./
COPY --from=builder --chown=node:node /app/.next/static ./.next/static

USER node
EXPOSE 3000

CMD ["sh", "-c", "HOSTNAME=0.0.0.0 PORT=${PORT:-10000} node server.js"]