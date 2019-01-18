#!/usr/bin/python
import subprocess
import os
import mistune

for root, dirs, files in os.walk("/home/karl/jgit/data"):
    for file in files:
        if file.lower().endswith(".md"):
            full_path = os.path.join(root, file)
            text = open(full_path).read()
            html = mistune.markdown(text, escape=False)
            renamed_path = full_path.replace("/", "@");
            with open("/tmp/mdtest/" + renamed_path + ".mdorig", "w") as f:
                f.write(text)
            with open("/tmp/mdtest/" + renamed_path + ".mistune", "w") as f:
                f.write(html)


for root, dirs, files in os.walk("/tmp/mdtest"):
    for file in files:
        if file.lower().endswith(".mdorig"):
            full_path = os.path.join(root, file)
            old_path = full_path.replace(".mdorig", ".mistune")
            new_path = full_path.replace(".mdorig", ".net")
            p = subprocess.Popen(["diff", old_path, new_path], 
                    stdout=subprocess.PIPE)
            out, err = p.communicate()
            rc = p.wait()
            print "%s: rc=%d" % (full_path, rc)
            if rc != 0:
                print out
