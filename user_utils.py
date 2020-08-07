# =============================================================================
# Generate class diagram
# =============================================================================

! pyreverse -o png .

# =============================================================================
# Change indentation
# =============================================================================

import re,os

for path, subdirs, files in os.walk("./"):
  for name in files:
    if name[-3:] == ".py":
      pass


with open(os.path.join(path, name)) as fp:
  text = fp.read()

def gen_replacement_string(match):
  print(match)
  print(match.groups())
  return '  '*(len(match.groups()[0])//4)

with open(os.path.join(path, name), 'w') as fp:
  fp.write(re.sub(r'^((?:    )+)', gen_replacement_string, text, flags=re.MULTILINE))



ff = "./main.py"

with open(ff) as fp:
  text = fp.read()

def gen_replacement_string(match):
  print(match)
  print(match.groups())
  return '  '*(len(match.groups()[0])//4)

with open(ff, 'w') as fp:
  fp.write(re.sub(r'^((?:    )+)', gen_replacement_string, text, flags=re.MULTILINE))


# =============================================================================
# Statistics about files
# =============================================================================

import re,os
import pandas as pd

tot = 0
files_stats = {}

for path, subdirs, files in os.walk("./"):
  for name in files:
    if name[-3:] == ".py":

      with open(os.path.join(path, name), "r") as f:
        lines = f.readlines()
        tot += len(lines)
        files_stats[name] = {"NumLines" : len(lines)}

stats = pd.DataFrame(files_stats).T

