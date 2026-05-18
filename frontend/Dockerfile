FROM node:20-alpine

WORKDIR /app

# Install dependencies first for layer caching
COPY package*.json ./
RUN npm install

COPY . .

EXPOSE 5173

# vite.config.ts already sets host: '0.0.0.0' and usePolling: true for Docker
CMD ["npm", "run", "dev"]
