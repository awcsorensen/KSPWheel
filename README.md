# KSPWheel _0.17.0.0_
## An update and feature enrichment of the original _**KSPWheel**_ plugin by _shadowmage45_

### Original KSPWheel can be found here including original feature list: https://github.com/shadowmage45/KSPWheel

##### _**As with the original KSPWheel plugin, this plugin does nothing without the part mods that require this purely for functions and features.**_

## Why update?
KSPWheel 0.16.14.33 simply didn't have the capabilities I required for KerbalFoundriesExpansion 1.3.0+ 

The no housing (NH) versions of the ALG needed to have much more user control over parameters to more closely fulfill the title of Adjustable Landing Gear. 

While I was modifying the plugin it was a good time to add some QoL changes to the functionality & GUI.  

## Fork Compatibility
Pre-release testing showed no conflicts with KerbalFoundries2, existing mods should function as normal. 

Mods using KSPWheelAdjustableGear and KSPWheelBogey will benefit from the new features if enabled within each parts config file. 

All new elements except the KSPWheelAdjustableGear smooth animation module are disabled by default and _**must be enabled**_ in the part config to use.

## Additional Fork Features:
* Expanded capabilities for user adjustment under the KSPWheelAdjustableGear and KSPWheelBogey modules.

- **KSPWheelAdjustableGear:**
  - User adjustable main strut retract angle
  - User adjustable off axis retract angle
  - User adjustable secondary strut retract angle
  - User adjustable wheel bogey retract angle
  - Lock user set extension in retract animation
  - Reset retract settings to default config values

- **KSPWheelBogey:**
  - User adjustable bogey droop angle
  - User adjustable bogey angle inversion
  - KSPWheelAdjustableGear integration
  - Reset bogey droop to default config values
    
- **GUI:**
  - Show Wheel Controls hidden by default
  - Configurable setting visibility
  - Reset controls for user-adjusted values
  - Improved wheel-angle display and adjustment

- **Additional Functionality:**
  - Symmetry syncing between mirrored pairs
  - Configurable setting visibility
 
## Source Code License
Source code for this project is currently licensed under GPL3.0 (or later).  Please see the accompanying License-GPL3.txt file for full licensing details.  Or alternatively the license may be viewed online at https://www.gnu.org/licenses/gpl-3.0.txt

## Configs Licensing
Config files, if applicable, are hereby released into the Public Domain, and are free to be used, altered, and redistributed.
