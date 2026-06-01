# -*- coding: utf-8 -*-
import zipfile, re
import xml.etree.ElementTree as ET

WORK = r"C:\Users\idan9\projects\projectFinal\.book_work"
MINE_DOCX = r"C:\Users\idan9\Downloads\Online_Checkers_ספר_פרויקט.docx"
WURI = 'http://schemas.openxmlformats.org/wordprocessingml/2006/main'
W = '{%s}' % WURI
ET.register_namespace('w', WURI)
# register other common namespaces to keep prefixes clean
for pfx,uri in [
 ('r','http://schemas.openxmlformats.org/officeDocument/2006/relationships'),
 ('m','http://schemas.openxmlformats.org/officeDocument/2006/math'),
 ('wp','http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing'),
 ('a','http://schemas.openxmlformats.org/drawingml/2006/main'),
 ('pic','http://schemas.openxmlformats.org/drawingml/2006/picture'),
 ('wps','http://schemas.microsoft.com/office/word/2010/wordprocessingShape'),
 ('mc','http://schemas.openxmlformats.org/markup-compatibility/2006'),
 ('v','urn:schemas-microsoft-com:vml'),
 ('w10','urn:schemas-microsoft-com:office:word'),
 ('o','urn:schemas-microsoft-com:office:office'),
 ('wpg','http://schemas.microsoft.com/office/word/2010/wordprocessingGroup'),
]:
    ET.register_namespace(pfx, uri)

with zipfile.ZipFile(MINE_DOCX) as z:
    raw = z.read('word/document.xml')

root = ET.fromstring(raw)
body = root.find(W+'body')
kids = list(body)

def text_of(p):
    return ''.join(t.text or '' for t in p.iter(W+'t')).strip()

def style_of(p):
    ppr = p.find(W+'pPr')
    if ppr is None: return ''
    ps = ppr.find(W+'pStyle')
    return ps.get(W+'val','') if ps is not None else ''

def xml_of(p):
    return ET.tostring(p, encoding='unicode')

out = []
out.append('TOTAL body children: %d' % len(kids))
# list first 20 paragraph-ish children
out.append('--- first 20 children (tag | style | text[:50]) ---')
for i,p in enumerate(kids[:20]):
    tag = p.tag.replace(W,'w:')
    out.append('%3d %-8s [%s] %s' % (i, tag, style_of(p) if p.tag==W+'p' else '-', text_of(p)[:50] if p.tag==W+'p' else ''))

# find index of first paragraph whose text starts with 'תקציר' (abstract) and 'פרק 1'
def find_first(pred):
    for i,p in enumerate(kids):
        if p.tag==W+'p' and pred(text_of(p)):
            return i
    return -1
i_abs = find_first(lambda t: t.startswith('תקציר'))
i_ch1 = find_first(lambda t: t.startswith('פרק 1'))
out.append('index of תקציר: %d ; index of פרק 1: %d' % (i_abs, i_ch1))

# last child (sectPr?)
out.append('last child tag: %s' % kids[-1].tag.replace(W,'w:'))

# write templates: one H1, H2, H3, a normal body para, a bold para
def first_with_style(s):
    for p in kids:
        if p.tag==W+'p' and style_of(p)==s and text_of(p):
            return p
    return None
templates = {}
templates['H1'] = first_with_style('1')
templates['H2'] = first_with_style('2')
templates['H3'] = first_with_style('3')
# normal body: style '' with long text
nb=None
for p in kids:
    if p.tag==W+'p' and style_of(p)=='' and len(text_of(p))>60:
        nb=p; break
templates['BODY']=nb
# bold para: a run with <w:b/> and short text
bp=None
for p in kids:
    if p.tag==W+'p':
        rpr = p.find('.//'+W+'rPr')
        if rpr is not None and rpr.find(W+'b') is not None and 0<len(text_of(p))<40:
            bp=p; break
templates['BOLD']=bp

with open(WORK+r"\templates.txt",'w',encoding='utf-8') as f:
    f.write('\n'.join(out))
    f.write('\n\n===== TEMPLATE XML =====\n')
    for k in ['H1','H2','H3','BODY','BOLD']:
        f.write('\n----- %s -----\n' % k)
        f.write(xml_of(templates[k]) if templates[k] is not None else 'NONE')
        f.write('\n')
    f.write('\n----- SECTPR (last child) -----\n')
    f.write(xml_of(kids[-1]))

# also dump raw XML of cover paragraphs 0..(i_abs-1) to a file for keeping
with open(WORK+r"\cover_paras.xml",'w',encoding='utf-8') as f:
    for p in kids[:max(i_abs,0)]:
        f.write(xml_of(p))
        f.write('\n')

print('\n'.join(out[:6]))
print('wrote templates.txt and cover_paras.xml')
