FROM node:22-alpine AS build
WORKDIR /app

COPY src/TradeCopilot.Client/package*.json ./
RUN npm ci

COPY src/TradeCopilot.Client ./
RUN npm run build

FROM nginx:1.27-alpine AS final
COPY src/TradeCopilot.Client/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
