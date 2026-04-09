# MergeNow
Visual Studio 2022 and 2026 extension for making quick TFVC merges.\
Pick a changeset and merge it directly to selected target branch:

![MergeNow view](https://github.com/kamil-wachala/MergeNow/blob/main/docs/MergeNowView.png)

# Installation
Get the extension from [here](https://marketplace.visualstudio.com/items?itemName=KamilWachala.MergeNow10) and double clik on the VSIX file.

# Requirements
* Microsoft Visual Studio 2022 or 2026 installed
* .NET Framework 4.8 or later installed

# Cool Features
* Direct access from Pending Changes page in Visual Studio
  
  ![MergeNow view](https://github.com/kamil-wachala/MergeNow/blob/main/docs/PendingChanges.png)

* Find changeset by:
  
  * Typing it's number
    
  * Browsing using VS Find Changeset dialog
    
     ![MergeNow view](https://github.com/kamil-wachala/MergeNow/blob/main/docs/FindChangeset.png)
    
  * Finding a changeset in VS View History window and selecting Merge Now from right click context menu
    
    ![MergeNow view](https://github.com/kamil-wachala/MergeNow/blob/main/docs/History.png)
    
* See found changeset details when clicked on a link
  
   ![MergeNow view](https://github.com/kamil-wachala/MergeNow/blob/main/docs/ChangesetDetails.png)
  
* Set default merge comment format. Go to Tools/Options in VS and look for Merge Now settings section.
  
![MergeNow view](https://github.com/kamil-wachala/MergeNow/blob/main/docs/Settings.png)

* Advanced menu:
  
  * Combined merge - merge a changeset to several target branches and check-in all at once. Comment will be amended with all target branch names, instead of beaing replaced.
    
  * Clear Pending Changes - clears comment, clears associated workitems and excludes all pending changes. Won't undo pending changes.
