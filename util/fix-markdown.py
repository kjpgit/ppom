#!/usr/bin/python
import os
import re

for root, dirs, files in os.walk("/home/karl/jgit/data"):
    for file in files:
        if file.lower().endswith(".md"):
            full_path = os.path.join(root, file)
            print full_path
            text = open(full_path).read()

            text2 = re.sub(r"^([#]{2,})(\w)", r"\1 \2", text, 0, re.MULTILINE)
            if (text != text2):
                open(full_path, "w").write(text2)




