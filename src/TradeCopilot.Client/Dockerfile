FROM node:22-alpine AS build
WORKDIR /app
ARG VITE_AUTH_AUTHORITY
ARG VITE_AUTH_CLIENT_ID
ARG VITE_AUTH_SCOPE
ARG VITE_API_BASE_URL
ARG VITE_APP_BASE_PATH
ENV VITE_AUTH_AUTHORITY=$VITE_AUTH_AUTHORITY
ENV VITE_AUTH_CLIENT_ID=$VITE_AUTH_CLIENT_ID
ENV VITE_AUTH_SCOPE=$VITE_AUTH_SCOPE
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL
ENV VITE_APP_BASE_PATH=$VITE_APP_BASE_PATH

COPY src/TradeCopilot.Client/package*.json ./
RUN npm ci

COPY src/TradeCopilot.Client ./
RUN npm run build

FROM nginx:1.27-alpine AS final
COPY src/TradeCopilot.Client/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
