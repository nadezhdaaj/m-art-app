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

# запускаем сервер
CMD ["node", "index.js"]