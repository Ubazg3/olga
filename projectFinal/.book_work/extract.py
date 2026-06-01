# -*- coding: utf-8 -*-
import zipfile, os, re, email, html as htmlmod

WORK = r"C:\Users\idan9\projects\projectFinal\.book_work"
MINE_DOCX = r"C:\Users\idan9\Downloads\Online_Checkers_ספר_פרויקט.docx"
EX_DOCX   = r"C:\Users\idan9\Downloads\Project Book.docx"

os.makedirs(WORK, exist_ok=True)

def unzip(src, dst):
    os.makedirs(dst, exist_ok=True)
    with zipfile.ZipFile(src) as z:
        z.extractall(dst)
    return dst

def html_from_mht(mht_path):
    with open(mht_path, 'rb') as f:
        msg = email.message_from_binary_file(f)
    for part in msg.walk():
        if part.get_content_type() == 'text/html':
            payload = part.get_payload(decode=True)
            cs = part.get_content_charset() or 'utf-8'
            return payload.decode(cs, errors='replace')
    return None

report = []

# ---- user's doc ----
mu = unzip(MINE_DOCX, os.path.join(WORK, 'mine_unpacked'))
mht = os.path.join(mu, 'word', 'afchunk.mht')
report.append('mine afchunk.mht exists: %s' % os.path.exists(mht))
H = html_from_mht(mht)
open(os.path.join(WORK,'mine_full.html'),'w',encoding='utf-8').write(H)
report.append('mine html chars: %d' % len(H))

# head
head = re.search(r'(?is)<head.*?</head>', H)
open(os.path.join(WORK,'mine_head.html'),'w',encoding='utf-8').write(
    re.sub(r'>(?=<)', '>\n', head.group(0)) if head else 'NOHEAD')

# body
body = re.search(r'(?is)<body.*?</body>', H).group(0)
open(os.path.join(WORK,'mine_body_pretty.html'),'w',encoding='utf-8').write(
    re.sub(r'>(?=<)', '>\n', body))

# locate cover/TOC boundary
itoc = body.find('תוכן עניינים')  # "תוכן עניינים"
report.append('TOC label index in body: %d' % itoc)
# show 900 chars before TOC label to find the page break that ends the cover
open(os.path.join(WORK,'cover_tail.html'),'w',encoding='utf-8').write(
    re.sub(r'>(?=<)', '>\n', body[max(0,itoc-1400):itoc+200]))

# list MIME parts of mine (to know images embedded)
with open(mht,'rb') as f:
    msg = email.message_from_binary_file(f)
parts=[]
for part in msg.walk():
    parts.append('%s | loc=%s | enc=%s' % (part.get_content_type(),
                 part.get('Content-Location'), part.get('Content-Transfer-Encoding')))
open(os.path.join(WORK,'mine_mime_parts.txt'),'w',encoding='utf-8').write('\n'.join(parts))

# ---- example doc ----
eu = unzip(EX_DOCX, os.path.join(WORK, 'example_unpacked'))
emht = os.path.join(eu, 'word', 'afchunk.mht')
report.append('example afchunk.mht exists: %s' % os.path.exists(emht))
if os.path.exists(emht):
    EH = html_from_mht(emht)
    open(os.path.join(WORK,'example_full.html'),'w',encoding='utf-8').write(EH)
    ehead = re.search(r'(?is)<head.*?</head>', EH)
    open(os.path.join(WORK,'example_head.html'),'w',encoding='utf-8').write(
        re.sub(r'>(?=<)', '>\n', ehead.group(0)) if ehead else 'NOHEAD')
    # plain text of example body for structure study
    eb = re.search(r'(?is)<body.*?</body>', EH).group(0)
    t = re.sub(r'(?is)<(/?)(p|div|h[1-6]|br|li|tr)[^>]*>', '\n', eb)
    t = re.sub(r'(?s)<[^>]+>', '', t)
    t = htmlmod.unescape(t)
    lines=[ln.strip() for ln in t.splitlines()]
    out=[]; blank=0
    for ln in lines:
        if not ln:
            blank+=1
            if blank<=1: out.append('')
        else:
            blank=0; out.append(ln)
    open(os.path.join(WORK,'example_text.txt'),'w',encoding='utf-8').write('\n'.join(out))
    report.append('example html chars: %d' % len(EH))

open(os.path.join(WORK,'report.txt'),'w',encoding='utf-8').write('\n'.join(report))
print('\n'.join(report))
print('DONE')
