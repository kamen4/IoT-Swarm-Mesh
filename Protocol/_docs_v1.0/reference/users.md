# Users & roles

## Users

IoT is a closed system so there must be a restricted and very limited access on who can rule this

> [!important] Rule
> The first user connected to the bot with a `/start` command is an admin.

So that if the real admin wasn't a first user, he can reset the telegram bot and try unlimited number of times.

## Register users

An admin in any moment can CRUD users from the telegram bot. He can add user by sending a mention so that next time this user will write a message to the bot he will be added to system. Also admin can grant dedicated_admin role to any other user. So here are 3 roles matrix:

**Users**
Can send requests to devices and read info about devices

**Dedicated Admins**
Can do what users do. Also can CRUD devices and other users

**Admins**
Can do what dedicated admins do. Also can grant and withdraw dedicated admin roles
