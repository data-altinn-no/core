#!/bin/sh
grep 'GetSetting("' Settings.cs | sed -rn 's/^.*GetSetting\("(\w+)"\).*/\1/p' | sort -u > usedkeys
grep -Po '^\s*"\w*?":' ../local.settings.json | sed -r 's/^\s*"(\w*)":/\1/' | sort -u > foundkeys
echo "Settings.cs\t\t\t\t\t\t\tlocal.settings.json"
echo "-----------------------------------------------------------------------------------------------------------------------------------"
diff -y usedkeys foundkeys | grep -Pv 'AzureWebJobsStorage|FUNCTIONS_WORKER_RUNTIME|IsEncrypted|Values'
rm usedkeys foundkeys
