# CountryBot

Support Server Invite has been updated: 16th January 2023

Support Server: https://discord.gg/2Sng2enNFW
Invite Link: https://discord.com/api/oauth2/authorize?client_id=992112299894636614&permissions=268486656&scope=bot%20applications.commands

CountryBot is a bot that allows your users to set a country role within your Discord guild. The roles are created dynamically and as required, if the role is no longer needed, it is removed. If your guild is boosted, the countries flag will also be set as the roles icon so it displays next to users names in your chat. 

This bot will not attempt to create a role if your guild has reached it's role cap (about 250 roles).

**The commands are as follows:**

`/help` - Shows a message on what commands are available to use.

`/choose` - This command would be run by a server operator. Shows an easier to use message that allows users to select a country/region from what letter it starts with, it will then ask the user which country from that letter. This is the preferred option for ease of use and doesn't require typing any commands once the message is available to users.

![Example](https://cdn.discordapp.com/attachments/1064500230512451635/1069197108403519588/image.png)

`/set <countryCode>` - Sets the user to the role based on the country code. 

`/search <country>` - Not sure what your countries code is? Use this to find it. 

`/remove` - Removes the country role. If the role is not used by anyone, it is deleted. 

`/stats <bool>` - `/stats` on it's own will show the stats for your guild, `/stats true` will show the stats worldwide. 

**Admins also have:**

`/admin purge` - This will remove all roles from the guild created by this bot and also delete any user data we have in the database. 

`/admin flags` - If your server is boosted (tier 2 or 3), you can choose if your guild automatically adds the country flag to the role, which will appear to the right of users who are in roles that have no other icons that take priority. 

User Data stored is just the user id and the role id tied together so the bot can keep track of who is in which role.
