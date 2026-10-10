# GoVee Smart Dashboard

Windows Avalonia MVVM dashboard for a GoVee H5086 smart plug through REST and H5110 thermometers through Bluetooth LE.

## Devices
- Plug 1 — H5086 — REST
- Inside — H5110 — Bluetooth LE
- Lanai — H5110 — Bluetooth LE

## Requirements
- Windows 10 19041+ or Windows 11
- .NET 8 SDK
- Bluetooth adapter
- GoVee Developer API key

## Run
```powershell
dotnet restore
dotnet run
```

The app automatically discovers/polls on startup. The polling interval defaults to 15 seconds and is configurable from Settings.

## REST API
Uses:
- GET `/router/api/v1/user/devices`
- POST `/router/api/v1/device/state`
- POST `/router/api/v1/device/control`

## Bluetooth
The H5110 is a BLE temperature/humidity sensor. This project uses the Windows `BluetoothLEAdvertisementWatcher` and decodes the H5110 advertisement payload. Since both H5110 units advertise the same model, their Bluetooth addresses are persisted so Inside and Lanai can be mapped independently.

## Persistence
Settings are stored at:
`%LOCALAPPDATA%\GoveeSmartDashboard\settings.json`

For production, replace the plain-text API key storage with DPAPI or Windows Credential Manager.

## Important
GoVee's cloud API and the H5110 BLE protocol are different transports. H5110 readings are obtained locally over Bluetooth; the H5086 is controlled and polled through GoVee's REST API.

The BLE parsing is based on the maintained `Bluetooth-Devices/GoVee-ble` implementation, which recognizes H5110 advertisements.
