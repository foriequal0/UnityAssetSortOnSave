# Unity Asset Sort On Save
Sort Unity asset files on save to ease merge difficulty.
This UnityEditor script will sort your Scene(.unity) and Prefab(.prefab) files automatically right after you save file.

## How it works
* Get Asset files to be saved using AssetModificationProcessor.OnWillSaveAssets()
* Watch until Unity finally saves. (Known to be scrambled by Unity)
* Sort by entity tag (looks like "--- !u!12 &12345678")
* Reload silently.

## Tested?
Unity 5. simple scenes & prefabs.

## Effective?
I don't know.

## Pull Request
Welcome

## Reference
* http://www.gamasutra.com/blogs/AaronSmith/20140917/225779/Unity_Scene_Diff_Tool.php
