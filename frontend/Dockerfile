# Build stage
FROM node:22-alpine AS build
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

COPY . .

# Browser runs on the host, so API URL must be the published host port (5192 -> container 8080)
ARG VITE_API_BASE_URL=http://localhost:5192
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL

RUN npm run build

# Runtime stage
FROM nginx:1.27-alpine AS runtime
ARG VITE_API_BASE_URL=http://localhost:5192
ENV API_ORIGIN=$VITE_API_BASE_URL
COPY nginx.conf /etc/nginx/templates/default.conf.template
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
