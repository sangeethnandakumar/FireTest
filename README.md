# Twileloop.FireTest

## 1. Installation

> Install on any machine that has .NET 6 installed

#### Install
```powershell
dotnet tool install --global firetest
```
#### Update
```powershell
dotnet tool update --global firetest
```

## 2. Usage (Command Based)
Otherwise you can also drive `FireTest` with it's powerful command support

### To Run A Test
```powershell
firetest run car.yml -p 5 -i 7
```

### To List All YML Files (From the current directory)
```powershell
firetest list
```

### To Generate A Template File (In the current directory)
```powershell
firetest create 'car.yml'
```

## 3. Usage (Interactive)
To use interactively, Simply launch the app from anywhere by executing
```powershell
firetest
```
This will launch the app with an interactive menu you can play without messing up with commands

![image](https://github.com/sangeethnandakumar/FireTest/assets/24974154/127a7204-b7f7-4a40-a7bd-ca37816f2363)



