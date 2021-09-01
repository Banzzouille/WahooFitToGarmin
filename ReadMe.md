# Send Wahoo fit files to Garmin Connect
A docker image to synchronize fit files coming from Wahoo devices to GarminConnect using dropbox

# The problem i tried to solve
I have been a Garmin device user for a long time. I swithed to Wahoo bike unit after my 1030 died. But unfortunatly, there is no bridge from Wahoo Companion directly to Garmin Connect. I synchronize everything with Strava but use Garmin Connect to check my km done on each bike and componants.

I didn't want to continue to synchronize manually once a week my activity between these 2 plateforms.

# What you need

- I use auto export functionality to DropBox directly from Wahoo compagnon app
- Declare an application on Dropbox developer site web
- A scheduler to ping your docker container to upload activities to Garmin Connect

# Step by step Guide

## 1. On your wahoo compagnon app
You need to synchronize your Wahoo companion app with your dropbox account. If you don't have a Dropbox account, you can create one here : [https://www.dropbox.com](https://www.dropbox.com)

Then, go to your Wahoo app -> Profile -> Connected Apps -> find dropbox entry in the list -> connect the app to dropbox with your account

![DropBox-Wahoo-App.jpg](https://i.postimg.cc/mknWL7pb/Drop-Box-Wahoo-App.jpg)

Here, when you finish your next activty, a fit file will be directly send to your dropbox cloud account. You can also synchronize your past activities by clicking on "Send training" in the history tab.

## 2. Install your docker image

The docker image is available at this place : [https://hub.docker.com/r/banzzouille/wahoo-fit-to-garmin](https://hub.docker.com/r/banzzouille/wahoo-fit-to-garmin)

These are the parameters to use it.
it exposes the port 80.

```
docker run -d --name='wahoo-fit-to-garmin'
                -e 'DropboxAppName'='YourAppName' 
                -e 'DropboxAppToken'='YourAppToken' 
                -e 'DropboxAppSecret'=YourAppSecret' 
                -e 'GarminConnectUserName'='YourUserName' 
                -e 'GarminConnectPassword'='YourPassword' 
                'banzzouille/wahoo-fit-to-garmin' 
```
It's your reponsability to make it accessible from the internet because dropbox will ping your docker image when a new file will appear.
When a new fit file will be dropped in the "WahooFitness" folder, the docker image will download all the fit files found in this folder, and delete it.

## 3. Declare a DropBox app
You need to declare your own dropbox application  to be able to work with your file through this docker image.

First, register to this web site [https://www.dropbox.com/developers/](https://www.dropbox.com/developers/)

Click on Console App link

Create your own app.

![Dropbox-Create-App.jpg](https://i.postimg.cc/4xCjWfrF/Dropbox-Create-App.png)

In the permissions section, make your file readable and writable.

![Dropbox-read-write.jpg](https://i.postimg.cc/FzMCCpnJ/Dropbox-read-write.jpg)

Then copy your app key secret and generated token for futur uses.

![Dropbox-App-key-secret.jpg](https://i.postimg.cc/4NvL0PXz/Dropbox-App-key-secret.jpg)

Create a webhook to ping your image automatically when a new file arrives on your dropbox account
The end of this url has to be __/dropbox__

![Dropbox-webhook.jpg](https://i.postimg.cc/tgvKt3yF/Dropbox-webhook.jpg)

## 4. Automate the upload file to Garmin Connect

Today, I use tool to check and monitor my website named Ciao : [https://brotandgames.com/ciao/](https://brotandgames.com/ciao/)

I just add a new entry like this 
![ciao-config.jpg](https://i.postimg.cc/7Y1jQ8wH/ciao-config.jpg)

You have to ping an url finishing by __/garmin__ to upload one by one the previously downloaded fit files.
In this example, i do this every 5 minutes

With this installation, you will be now able to synchronize your Wahoo activities directly to Garmin Connect.

Thanks to [https://github.com/La0/garmin-uploader](https://github.com/La0/garmin-uploader) for the job.
