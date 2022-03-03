from mega import Mega

mega = Mega()
global m
m = mega.login()
m = mega.login("", "")

mBlend = m.find('Scene.Blend')
print(mBlend)
try:
    m.download(mBlend)
except PermissionError:
    print("fail")
