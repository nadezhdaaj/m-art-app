FROM node:20

WORKDIR /app

# копируем package.json
COPY package*.json ./

# устанавливаем зависимости
RUN npm install

# копируем весь проект
COPY . .

# генерируем prisma client
RUN npx prisma generate

# накатываем миграции и запускаем сервер
CMD npx prisma migrate deploy && node index.js