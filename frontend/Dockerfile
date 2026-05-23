# syntax=docker/dockerfile:1.7
# Frontend React + Vite — multi-stage para hom/prod
# Build: node 20 alpine → static dist/
# Serve: nginx 1.27 alpine com SPA routing e gzip
# Atende: RNF005 (performance), RNF007 (responsivo), RNF010 (visual)

ARG NODE_VERSION=20
ARG NGINX_VERSION=1.27

# ---------- Stage 1: build ----------
FROM node:${NODE_VERSION}-alpine AS build
WORKDIR /app

# Cache de dependências
COPY package.json package-lock.json* ./
RUN npm ci --no-audit --no-fund

# Build args expostos ao Vite (build-time, não runtime)
ARG VITE_API_URL
ENV VITE_API_URL=${VITE_API_URL}

COPY . .
RUN npm run build

# ---------- Stage 2: nginx ----------
FROM nginx:${NGINX_VERSION}-alpine AS final

# Usuário não-root: nginx alpine já tem o usuário 'nginx', mas precisamos
# ajustar permissões para conseguir gravar pid/cache em paths não-privilegiados.
# Sem o chown abaixo o master falha com "mkdir() /var/cache/nginx/client_temp (13)".
RUN sed -i 's,listen       80;,listen       8080;,' /etc/nginx/conf.d/default.conf 2>/dev/null || true \
    && sed -i 's,/var/run/nginx.pid,/tmp/nginx.pid,' /etc/nginx/nginx.conf \
    && sed -i 's,/run/nginx.pid,/tmp/nginx.pid,' /etc/nginx/nginx.conf \
    && sed -i 's,user  nginx;,,' /etc/nginx/nginx.conf \
    && chown -R nginx:nginx /var/cache/nginx /var/log/nginx /etc/nginx/conf.d

COPY --chown=nginx:nginx nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build --chown=nginx:nginx /app/dist /usr/share/nginx/html

EXPOSE 8080

HEALTHCHECK --interval=15s --timeout=5s --start-period=10s --retries=3 \
    CMD wget --quiet --spider http://localhost:8080/ || exit 1

USER nginx
CMD ["nginx", "-g", "daemon off;"]
