from mega import Mega

mega = Mega()
global m
m = mega.login()
m = mega.login("", "")

m_blend = m.find('Scene.Blend')
print(m_blend)
try:
    m.download(m_blend)
except PermissionError:
    print("fail")
