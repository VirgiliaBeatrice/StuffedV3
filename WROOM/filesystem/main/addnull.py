import sys
args = sys.argv;
args.pop(0)
for fname in args:
	contents = open(fname, "rb").read()
	if contents[-1] != 0 :
		open(fname, "ab").write(b'\0')
 