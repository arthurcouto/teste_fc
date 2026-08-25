FROM nginxinc/nginx-unprivileged:1.27-alpine
COPY default.conf /etc/nginx/conf.d/default.conf
COPY index.html styles.css app.js /usr/share/nginx/html/
EXPOSE 8080
