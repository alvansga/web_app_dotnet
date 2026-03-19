## How to web hosting ke ngrok
---

```
dotnet run --urls="http://0.0.0.0:5253"
```

Install ngrok

```
ngrok http 5253

or

ngrok http --url=apparently-becoming-octopus.ngrok-free.app 5253    

or 

# setting di C:\Users\user\AppData\Local\ngrok\ngrok.yml

    tunnels:
        webapp:
            proto: http
            addr: 5253
            domain: apparently-becoming-octopus.ngrok-free.app
        websocket:
            proto: http
            addr: 8000

kemudian jalankan,  sesuai nama tunnels (webapp):
ngrok start webapp
```
