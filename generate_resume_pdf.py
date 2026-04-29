from pathlib import Path

lines = [
    b'%PDF-1.4\n',
    b'1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n',
    b'2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n',
    b'3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >>\nendobj\n',
    b'4 0 obj\n<< /Length 92 >>\nstream\nBT\n/F1 24 Tf\n72 720 Td\n(Resume placeholder for Lauritz Fokdal Koch) Tj\nET\nendstream\nendobj\n',
    b'5 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>\nendobj\n',
]

offsets = []
pos = 0
for line in lines:
    offsets.append(pos)
    pos += len(line)

xref = b'xref\n0 6\n0000000000 65535 f \n'
for off in offsets:
    xref += f'{off:010d} 00000 n \n'.encode('ascii')

trailer = b'trailer\n<< /Size 6 /Root 1 0 R >>\nstartxref\n'
startxref = str(pos).encode('ascii') + b'\n%%EOF\n'
content = b''.join(lines) + xref + trailer + startxref
Path('resume.pdf').write_bytes(content)
print('resume.pdf created, size', len(content))
