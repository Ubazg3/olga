# -*- coding: utf-8 -*-
import zipfile, os, re, email
import xml.etree.ElementTree as ET

WORK = r"C:\Users\idan9\projects\projectFinal\.book_work"
MINE_DOCX = r"C:\Users\idan9\Downloads\Online_Checkers_ספר_פרויקט.docx"
EX_DOCX   = r"C:\Users\idan9\Downloads\Project Book.docx"
W = '{http://schemas.openxmlformats.org/wordprocessingml/2006/main}'

def read_zip_member(path, member):
    with zipfile.ZipFile(path) as z:
        try:
            return z.read(member)
        except KeyError:
            return None

def para_outline(doc_xml_bytes):
    root = ET.fromstring(doc_xml_bytes)
    body = root.find(W+'body')
    lines = []
    if body is None:
        return lines
    for p in body.iter(W+'p'):
        ppr = p.find(W+'pPr')
        style = ''
        jc = ''
        if ppr is not None:
            ps = ppr.find(W+'pStyle')
            if ps is not None:
                style = ps.get(W+'val','')
            j = ppr.find(W+'jc')
            if j is not None:
                jc = j.get(W+'val','')
        # gather text + run props
        texts = []
        bold = False
        sizes = set()
        fonts = set()
        for r in p.iter(W+'r'):
            rpr = r.find(W+'rPr')
            if rpr is not None:
                if rpr.find(W+'b') is not None: bold = True
                sz = rpr.find(W+'sz')
                if sz is not None: sizes.add(sz.get(W+'val',''))
                rf = rpr.find(W+'rFonts')
                if rf is not None:
                    for a in ('ascii','hAnsi','cs'):
                        v = rf.get(W+a)
                        if v: fonts.add(v)
            for t in r.iter(W+'t'):
                texts.append(t.text or '')
        txt = ''.join(texts).strip()
        tag = '[%s|%s|b=%s|sz=%s|f=%s]' % (style or '-', jc or '-', 'Y' if bold else 'n',
              ','.join(sorted(sizes)) or '-', ','.join(sorted(fonts)) or '-')
        if txt:
            lines.append(tag + ' ' + txt)
        else:
            # note empty paragraphs / breaks
            br = p.find('.//'+W+'br')
            lines.append(tag + ' (empty)')
    return lines

def html_from_mht(path):
    with open(path,'rb') as f:
        msg = email.message_from_binary_file(f)
    for part in msg.walk():
        if part.get_content_type()=='text/html':
            return part.get_payload(decode=True).decode(part.get_content_charset() or 'utf-8', errors='replace')
    return None

def outline_for(docx_path, tag):
    docxml = read_zip_member(docx_path, 'word/document.xml')
    lines = para_outline(docxml) if docxml else []
    total = sum(len(l) for l in lines)
    note = 'native document.xml (%d paragraphs)' % len(lines)
    if total < 200:
        # fallback to MHT
        with zipfile.ZipFile(docx_path) as z:
            tmp = os.path.join(WORK, tag+'_afchunk.mht')
            data = None
            for n in z.namelist():
                if n.lower().endswith('.mht'):
                    data = z.read(n); break
            if data:
                open(tmp,'wb').write(data)
                H = html_from_mht(tmp)
                open(os.path.join(WORK, tag+'_mht.html'),'w',encoding='utf-8').write(H)
                note = 'MHT-based (html %d chars) -> see %s_mht.html' % (len(H), tag)
    open(os.path.join(WORK, tag+'_outline.txt'),'w',encoding='utf-8').write('\n'.join(lines))
    return note, len(lines)

r1 = outline_for(MINE_DOCX, 'mine')
r2 = outline_for(EX_DOCX, 'example')
print('MINE   :', r1)
print('EXAMPLE:', r2)

# also dump heading style definitions from mine styles.xml
st = read_zip_member(MINE_DOCX, 'word/styles.xml')
if st:
    root = ET.fromstring(st)
    hs = []
    for s in root.iter(W+'style'):
        sid = s.get(W+'styleId','')
        nm = s.find(W+'name')
        nmv = nm.get(W+'val','') if nm is not None else ''
        if 'eading' in sid or 'eading' in nmv or sid in ('Title','TOCHeading') or sid.startswith('TOC'):
            hs.append('%s | name=%s' % (sid, nmv))
    open(os.path.join(WORK,'mine_heading_styles.txt'),'w',encoding='utf-8').write('\n'.join(hs))
    print('heading-ish styles:', len(hs))
print('DONE')
