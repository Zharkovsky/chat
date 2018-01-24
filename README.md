**Решение состоит из 7 проектов:**

1) AngelsChat.Client - Клиент

2) AngelsChat.Server - Сервер

3) AngelsChat.Shared - Контракты для клиента и сервера

4) AngelsChat.WpfServerConfiguratorApp - Конфигуратор сервера (запуск с админскими правами)

5) AngelsChat.WindowsService - Сервер в виде сервиса Windows

6) AngelsChat.ConsoleServer - Консольная реализая сервера (запуск с админскими правами)

7) AngelsChat.WpfClientApp - Клиентское приложение

**Быстрый запуск**

1) Сбилдить проекты

2) Запустить AngelsChat.WpfServerConfiguratorApp (с админскими правами)
2.1) Установить следующие конфигурации:

Ip: localhost
port: 9080
БД: Каталог - (localdb)\MSSQLLocalDB
        Имя - chatdatabase

3) Запустить AngelsChat.ConsoleServer (с админскими правами)

4) Запустить клиент (можно несколько)
4.1) Нажать шестеренку в верхнем левом углу и установить параметры   Ip: localhost       port: 9080
4.2) Зарегистрироваться
4.3) Авторизоваться

**Особенности работы**

При закрытии окна клиента приложение скрывается в трей (правый нижний угол где часы).
Следовательно чтобы выйти из учетной записи нужно вызвать контекстное меню правой кнопкой мыши и выбрать нужный пункт.

**В случае если нужно пересобрать проект** , необходимо закрыть AngelsChat.WpfClientApp.exe через диспетчер задач!
