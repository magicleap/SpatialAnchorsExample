# Spatial Anchors Example

## Overview

This project demonstrates how to use [Magic Leap 2’s Spatial Anchors API](https://developer-docs.magicleap.cloud/docs/guides/unity/perception/anchors/spatial-anchors-overview) to build an application that allows users to create 3D objects that persist in the same location across reboots. In this example, the information associated with the objects and Anchor IDs is stored as a JSON file in local storage. The example also shows how to automatically localize into a Space using a QR Code and the Spaces application.

| Requirements| |
|--| --|
|SDK Version | 1.3.0 |
|Unity Version| 2022.2.0b7 |

### Interactions

Once Localized, you can create persistent content using the Spatial Anchors:

- Press Trigger to create a Cube.
- Press the Menu button to create a Sphere
- Press the Bumper to remove the closest anchor and bound object.

## Configure Space Selection via QR Code

### Obtain Existing Space IDs

This section describes how to obtain the map IDs for the spaces that are stored locally on your device.

1. Connect your headset to your computer.
2. Open Command Prompt or Terminal, then run the following command to list the spaces saved on your device:

```shell
adb shell mlmapping -spaces
```

If you get an error message saying the ADB command is not found, [install ADB](https://developer-docs.magicleap.cloud/docs/guides/developer-tools/android-debug-bridge/adb-setup)

## Create a QR Code

This section describes how to create a QR code using the Map ID. You can use any tool you like, in this example, we will be using [this 3rd party github project](https://www.nayuki.io/page/qr-code-generator-library)

Using the [provided website](https://www.nayuki.io/page/qr-code-generator-library) or equivalent, generate a QR Code With the Map ID. Make sure to include the Map ID prefix and the curly brackets. By default the Map ID Prefix is set to `MAGICLEAP-ARCLOUD-MAP-ID:`

Example : `MAGICLEAP-ARCLOUD-MAP-ID:{eb43a3cb-14a6-7018-9133-f978a185f84d}`

## Code Overview

## Creating and Modifying Persistent Content

The `PersistentContent` folder contains all of the scripts related to saving and loading data from local storage. Data is saved in a key-value pair, with the Key targeting the Magic Leap 2’s Anchor ID and the value being JSON data.

To make development easier, any serializable class can be saved as long as it inherits from `IStoratgeBinding`. You can also add additional properties directly to the SimpleAnchorBinding if you wish.

### `Spatial Anchors Example`

Contains the logic associated with using Magic Leap 2’s spatial anchors to create persistent content. In addition to using standard Magic Leap 2 APIs the script also demonstrates how to use the custom `BindingsLocalStorage` class to save and load and remove data that is bound to an anchor’s ID.

### `IStorageBinding`

An interface that is used to make sure the data can be bound to an anchor.

### `BindingLocalStorage`

A static class that manages loading and saving the bindings to local storage.

### `SimpleAnchorBinding`

A simple example of using the IStorageBinding interface to create bindable data. In this example application the data contains an Anchor ID and a JSON String.

## Open Spaces with QR Codes

The `QRCodeSpaceIntent` object contains all of the scripts related to opening the spaces app while targeting a specific space ID.

### `QRCodeReader`

Detects QR Code markers and triggers a Unity Event for each detected QR Code

### `MLSpaceIntentLauncher`

Contains a public function called `TryLocalize(string qrCodeData)` that allows the QRCodeReader to pass the data embedded on a QR code. The script checks if the data contains a known prefix before trying to localize into it. This prefix can be changed in the inspector.
