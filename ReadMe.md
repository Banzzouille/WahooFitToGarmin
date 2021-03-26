# Send Wahoo fit files to Garmin Connect
A docker image to synchronize fit files coming from Wahoo devices to GarminConnect using dropbox

# The problem i tried to solve
I was a Garmin device user for a long time. I swith to Wahoo bike unit after my 1030 died. But unfortunatly, tere is no bidge from Wahoo Compagnon directly to Garmin Connect. I synchronise everything with Strava but use Garmin Connect to compute my km done on each bike.

I didn't want to continue to synchronize manually once a week my activity betwwen this 2 plateforms.

# What you need

- I use activity auto export to DropBox directly from Wahoo compagnon app
- Declare an application on Dropbox developer site web
- A scheduler to ping your docker container to upload activities to Garmin Connect

# Step by step Guide

## 1. On your wahoo compagnon app
You need to synchronize your Wahhoo compagnon app with your dropbox account. Iy you don't have a Dropbox account, you can create one [https://www.dropbox.com](https://www.dropbox.com)

Then, go to your Wahoo app -> Profile -> Connected Apps -> find dropbox entry in the list -> connect the app to dropbox with your account

![DropBox-Wahoo-App.jpg](/doc/DropBox-Wahoo-App.jpg)

Here, when you will finish your next activty, a fit file will be directly send to your dropbox cloud account.

## 2. Install your docker image

The docker image is available at this place : [https://hub.docker.com/repository/docker/banzzouille/wahoo-fit-to-garmin](https://hub.docker.com/repository/docker/banzzouille/wahoo-fit-to-garmin)

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
When a new fit file will be drop in the "WahooFitness" folder, the docker image will download all the fit files found in this folder, and delete it.

## 3. Declare a DropBox app
You need to declare your own dropbox application  to be able to work with your file throw this docker image.

First, register to this web site [https://www.dropbox.com/developers/](https://www.dropbox.com/developers/)

Click on Console App link

Create your own app.

![Dropbox-Create-App.jpg](/doc/Dropbox-Create-App.jpg)

In the permissions section, make your file readable and writable.
![Dropbox-read-write.jpg](/doc/Dropbox-read-write.jpg)

Then copy your app key secret and generated token for futur uses.
![Dropbox-App-key-secret.jpg](/doc/Dropbox-App-key-secret.jpg)

Create a webhook to ping you image automatically when a new file arrive on your dropbox account
The end of this url has to be __/dropbox__
![Dropbox-webhook.jpg](/doc/Dropbox-webhook.jpg)

## 4. Automate the upload file to Garmin Connect

Today, I use tool to check and monitor my website named Ciao : (https://brotandgames.com/ciao/)[https://brotandgames.com/ciao/]

I juste add a new entry like this 
![ciao-config.jpg](/doc/ciao-config.jpg)

You have to ping an url finishing by __/garmin__ to upload one by one the previously downloaded fit files.
In this example, i do this every 5 minutes

With this installation, you will benow able to synchronize your Wahoo activities directly to Garmin Connect.

Thanks to [https://github.com/La0/garmin-uploader](https://github.com/La0/garmin-uploader) for the job.
