# Build from the repository root: docker build -f web/Dockerfile .
FROM node:24-alpine AS build
WORKDIR /web

COPY web/package.json web/package-lock.json ./
RUN npm ci

COPY web/ .
# The image always talks to the real API through nginx, never to the in-browser mocks
ENV VITE_USE_MOCKS=false
RUN npm run build

FROM nginx:1-alpine AS runtime
COPY web/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /web/dist /usr/share/nginx/html
EXPOSE 80
