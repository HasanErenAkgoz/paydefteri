FROM node:22-alpine AS build
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

COPY . .

# Google's client id is public (it ships in the page), but it differs per
# deployment, so it is injected here instead of being committed. Left empty the
# app simply hides the Google button.
ARG GOOGLE_CLIENT_ID=""
RUN sed -i "s|googleClientId: ''|googleClientId: '${GOOGLE_CLIENT_ID}'|" src/environments/environment.prod.ts

RUN npm run build -- --configuration=production

FROM nginx:1.27-alpine AS final
COPY deploy/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/web/browser /usr/share/nginx/html
EXPOSE 80
