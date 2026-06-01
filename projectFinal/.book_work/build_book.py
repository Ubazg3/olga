# -*- coding: utf-8 -*-
import sys, io, zipfile, re, shutil, os
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')

WORK = r"C:\Users\idan9\projects\projectFinal\.book_work"
SRC_DOCX = r"C:\Users\idan9\Downloads\Online_Checkers_ספר_פרויקט.docx"
OUT_DOCX = r"C:\Users\idan9\Downloads\Online_Checkers_ספר_פרויקט_מעודכן.docx"
CONTENT  = WORK + r"\book_content.txt"

def esc(s):
    return s.replace('&','&amp;').replace('<','&lt;').replace('>','&gt;')

def is_heb(ch):
    return '֐' <= ch <= '׿'

def split_runs(text):
    """Split mixed Hebrew/Latin text into (segment, is_rtl) keeping neutrals with current run."""
    segs=[]; cur=''; cur_rtl=None
    for ch in text:
        if is_heb(ch):
            c=True
        elif ch.isascii() and ch.isalnum():
            c=False
        else:
            c = cur_rtl if cur_rtl is not None else True
        if cur_rtl is None:
            cur_rtl=c; cur=ch
        elif c==cur_rtl:
            cur+=ch
        else:
            segs.append((cur,cur_rtl)); cur=ch; cur_rtl=c
    if cur:
        segs.append((cur,cur_rtl))
    return segs

def runs_xml(text, rpr_inner):
    """One RTL run for the whole paragraph text. Letting Word's bidi engine
    resolve the mixed Hebrew/English keeps parentheses, periods and commas in
    their correct places (exactly what you get typing in Word), instead of us
    hand-splitting runs and forcing punctuation onto the wrong side."""
    rpr = '<w:rPr>'+rpr_inner+'<w:rtl/></w:rPr>'
    return '<w:r>'+rpr+'<w:t xml:space="preserve">'+esc(text)+'</w:t></w:r>'

# ---- paragraph builders (rPr fingerprints copied from the user's document) ----
def h1(text):
    # pageBreakBefore so every chapter starts on a fresh page (and no stray blank pages)
    rpr='<w:rFonts w:ascii="David" w:hAnsi="David" w:cs="David"/><w:b/><w:color w:val="7B1F1F"/><w:sz w:val="44"/>'
    ppr='<w:pPr><w:pStyle w:val="1"/><w:pageBreakBefore/><w:bidi/><w:rPr>'+rpr+'<w:rtl/></w:rPr></w:pPr>'
    return '<w:p>'+ppr+runs_xml(text,rpr)+'</w:p>'

def h2(text):
    rpr='<w:rFonts w:ascii="David" w:hAnsi="David" w:cs="David"/><w:b/><w:color w:val="1A1F2A"/>'
    ppr='<w:pPr><w:pStyle w:val="2"/><w:bidi/><w:rPr>'+rpr+'<w:rtl/></w:rPr></w:pPr>'
    return '<w:p>'+ppr+runs_xml(text,rpr)+'</w:p>'

def h3(text):
    rpr='<w:rFonts w:ascii="David" w:hAnsi="David" w:cs="David"/><w:b/><w:color w:val="1A1F2A"/><w:sz w:val="26"/>'
    ppr='<w:pPr><w:pStyle w:val="3"/><w:bidi/><w:rPr>'+rpr+'<w:rtl/></w:rPr></w:pPr>'
    return '<w:p>'+ppr+runs_xml(text,rpr)+'</w:p>'

def body(text):
    rpr='<w:rFonts w:ascii="David" w:hAnsi="David" w:cs="David"/>'
    ppr='<w:pPr><w:bidi/><w:jc w:val="both"/><w:rPr>'+rpr+'</w:rPr></w:pPr>'
    return '<w:p>'+ppr+runs_xml(text,rpr)+'</w:p>'

def bold(text):
    rpr='<w:rFonts w:ascii="David" w:hAnsi="David" w:cs="David"/><w:b/>'
    ppr='<w:pPr><w:bidi/><w:rPr>'+rpr+'<w:rtl/></w:rPr></w:pPr>'
    return '<w:p>'+ppr+runs_xml(text,rpr)+'</w:p>'

def img(text):
    t='<תמונה: '+text+'>' if text.strip() else '<תמונה>'
    rpr='<w:rFonts w:ascii="David" w:hAnsi="David" w:cs="David"/><w:b/><w:color w:val="7B1F1F"/>'
    ppr='<w:pPr><w:bidi/><w:spacing w:before="60" w:after="60"/><w:jc w:val="center"/><w:rPr>'+rpr+'<w:rtl/></w:rPr></w:pPr>'
    return '<w:p>'+ppr+runs_xml(t,rpr)+'</w:p>'

def code(text):
    rpr='<w:rFonts w:ascii="Consolas" w:hAnsi="Consolas" w:cs="Consolas"/><w:sz w:val="18"/>'
    ppr='<w:pPr><w:jc w:val="left"/><w:rPr>'+rpr+'</w:rPr></w:pPr>'
    return '<w:p>'+ppr+'<w:r><w:rPr>'+rpr+'</w:rPr><w:t xml:space="preserve">'+esc(text)+'</w:t></w:r></w:p>'

def spacer():
    return '<w:p><w:pPr><w:bidi/></w:pPr></w:p>'

def toc_title(text):
    # looks like H1 but NOT pStyle "1" so it is excluded from the TOC field
    rpr='<w:rFonts w:ascii="David" w:hAnsi="David" w:cs="David"/><w:b/><w:color w:val="7B1F1F"/><w:sz w:val="44"/>'
    ppr='<w:pPr><w:bidi/><w:spacing w:before="240" w:after="200"/><w:rPr>'+rpr+'<w:rtl/></w:rPr></w:pPr>'
    return '<w:p>'+ppr+runs_xml(text,rpr)+'</w:p>'

def page_break():
    return '<w:p><w:pPr><w:bidi/></w:pPr><w:r><w:br w:type="page"/></w:r></w:p>'

def toc_field():
    placeholder = '<w:r><w:rPr><w:rFonts w:cs="David"/><w:rtl/></w:rPr><w:t xml:space="preserve">לחצו כאן ובחרו "עדכן שדה" (F9) כדי למלא את תוכן העניינים ומספרי העמודים.</w:t></w:r>'
    return ('<w:p><w:pPr><w:bidi/><w:rPr><w:rFonts w:cs="David"/></w:rPr></w:pPr>'
            '<w:r><w:rPr><w:rFonts w:cs="David"/></w:rPr><w:fldChar w:fldCharType="begin"/></w:r>'
            '<w:r><w:rPr><w:rFonts w:cs="David"/></w:rPr><w:instrText xml:space="preserve"> TOC \\o "1-3" \\h \\z \\u </w:instrText></w:r>'
            '<w:r><w:rPr><w:rFonts w:cs="David"/></w:rPr><w:fldChar w:fldCharType="separate"/></w:r>'
            + placeholder +
            '<w:r><w:rPr><w:rFonts w:cs="David"/></w:rPr><w:fldChar w:fldCharType="end"/></w:r></w:p>')

# ---- parse content file ----
def parse_content(path):
    out=[]
    for raw_line in open(path, encoding='utf-8').read().split('\n'):
        line=raw_line.rstrip('\r')
        if line.startswith('#'):       # comment
            continue
        if line.strip()=='':
            out.append(spacer()); continue
        if line.startswith('=1 '): out.append(h1(line[3:].strip()))
        elif line.startswith('=2 '): out.append(h2(line[3:].strip()))
        elif line.startswith('=3 '): out.append(h3(line[3:].strip()))
        elif line.startswith('* '):  out.append(bold(line[2:].strip()))
        elif line.startswith('img '):out.append(img(line[4:].strip()))
        elif line.startswith('code '):out.append(code(line[5:]))
        elif line.startswith('. '):  out.append(body(line[2:].strip()))
        else:                         out.append(body(line.strip()))
    return ''.join(out)

# ---- assemble ----
chapters = parse_content(CONTENT)
toc_block = (toc_title('תוכן עניינים') + toc_field())
generated = toc_block + chapters

# ---- splice into document.xml ----
with zipfile.ZipFile(SRC_DOCX) as z:
    raw = z.read('word/document.xml').decode('utf-8')
    names = z.namelist()
    data = {n: z.read(n) for n in names}

# cover end = start of the abstract paragraph (first <w:t>תקציר</w:t>)
m = raw.find('<w:t>תקציר</w:t>')
assert m != -1, 'abstract anchor not found'
cover_end = raw.rfind('<w:p ', 0, m)
assert cover_end != -1, 'cover paragraph start not found'

# final sectPr (last child of body)
sect = raw.rfind('<w:sectPr')
assert sect != -1, 'sectPr not found'

new_raw = raw[:cover_end] + generated + raw[sect:]

# update settings.xml so Word refreshes the TOC/fields on open.
# updateFields must be placed in schema order: after characterSpacingControl,
# before footnotePr/endnotePr/compat/rsids/...  Insert before the earliest of those.
st = data['word/settings.xml'].decode('utf-8')
if 'updateFields' not in st:
    anchors = ['<w:footnotePr', '<w:endnotePr', '<w:compat', '<w:rsids',
               '<m:mathPr', '<w:themeFontLang', '<w:clrSchemeMapping',
               '<w:shapeDefaults', '<w:decimalSymbol', '</w:settings>']
    positions = [st.find(a) for a in anchors if st.find(a) != -1]
    pos = min(positions)
    st = st[:pos] + '<w:updateFields w:val="true"/>' + st[pos:]
data['word/settings.xml'] = st.encode('utf-8')

data['word/document.xml'] = new_raw.encode('utf-8')

# write new docx
if os.path.exists(OUT_DOCX):
    os.remove(OUT_DOCX)
with zipfile.ZipFile(OUT_DOCX, 'w', zipfile.ZIP_DEFLATED) as z:
    for n in names:
        z.writestr(n, data[n])

print('cover_end char:', cover_end, ' sect char:', sect)
print('generated paragraphs:', generated.count('<w:p>')+generated.count('<w:p '))
print('wrote', OUT_DOCX)
