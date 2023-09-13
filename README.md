 # Spatial Anchors Example

## Overview

This project demonstrates how to use [Magic Leap 2’s Spatial Anchors API](https://developer-docs.magicleap.cloud/docs/guides/unity/perception/anchors/spatial-anchors-overview) to build an application that allows users to create 3D objects that persist in the same location across reboots. In this example, the information associated with the objects and Anchor IDs is stored as a JSON file in local storage. The example also shows how to allow user to localize into a Space without leaving the application using the MLSpaces API.

| Requirements| Version|
|--| --|
|Magic Leap Unity SDK | 1.9.0 |
|Unity Editor| 2022.2.0f1 |

### Interactions

Once Localized, you can create persistent content using the Spatial Anchors:

- Press Trigger to create a Cube.
- Press the Menu button to create a Sphere
- Press the Bumper to remove the closest anchor and bound object.

## Creating and Modifying Persistent Content

The `Assets/PersistentContent/Scripts` directory contains all of the scripts related to saving and loading data from local storage. Data is saved in a key-value pair, with the key being the Magic Leap 2’s Anchor ID and the value being JSON data.

To make development easier, you can save any serializable class as long as it inherits from `IStorageBinding`. You can also add additional properties directly to the SimpleAnchorBinding if you wish.

## Code Overview

#### `Persistent Content Example`

This script demonstrates how to use the custom `BindingsLocalStorage` class to save, load and remove data that is bound to an anchor’s ID. It also handles the Magic Leap 2 input events and calls functions in the custom `AnchorManager` script to create, destroy and update spatial anchors and bind them to data in a local JSON file.

#### `Anchor Manager`

This script handles the logic associated with Magic Leap 2's spatial anchors. It queries the anchors on a background thread with the help of the `ThreadDispatcher` utility script. It keeps track of the queried anchors and provides callbacks for the following actions: `OnAnchorUpdated`, `OnAnchorAdded`, `OnAnchorRemoved` , `OnAnchorsLost`. The script is generic and can be reused in other projects. The script also uses the MLSpaces  API to localize into a Space and update the anchors based on the user's localization status.

- `OnAnchorAdded`  - Triggered when an anchor is first tracked by the Anchor Manager.
- `OnAnchorUpdated` - Triggered when an anchor is refreshed after already being created.
- `OnAnchorRemoved` - Triggered when an anchor is deleted.
- `OnAnchorsLost` - Triggered when the user localizes into a new space or when both head pose and localization are lost.

#### `IStorageBinding`

An interface that is used to ensure that the data can be bound to an anchor.

#### `BindingLocalStorage`

A static class that manages loading and saving the bindings to local storage.

#### `SimpleAnchorBinding`

A simple example of using the IStorageBinding interface to create bindable data. In this example application, the data contains an Anchor ID and a JSON string.

#### `SpaceSelector`

A simple script that allows users to select a space from a dropdown and localize into it.

#### `LocalizationStatusDisplay`

A simple script used to display localization status on a TMP Text.

#### `ThreadDispatcher`

A utility script that handles dispatching calls from the Magic Leap native thread to the Unity thread. Used to search for anchors on a background thread to avoid performance overhead.
