import { useEffect, useState } from 'react'
import { Document, Page, pdfjs } from 'react-pdf'
import type { Bildiri } from './api'

pdfjs.GlobalWorkerOptions.workerSrc = new URL(
  'pdfjs-dist/build/pdf.worker.min.mjs',
  import.meta.url,
).toString()

type Props = {
  url: string
  title: string
  papers: Bildiri[]
}

export function PdfViewer({ url, title, papers }: Props) {
  const [data, setData] = useState<ArrayBuffer | null>(null)
  const [page, setPage] = useState(1)
  const [pages, setPages] = useState(0)
  const [width, setWidth] = useState(640)
  const [loadError, setLoadError] = useState<string | null>(null)

  useEffect(() => {
    let active = true
    setData(null)
    setLoadError(null)
    fetch(url)
      .then(async (response) => {
        if (!response.ok) throw new Error('PDF alınamadı.')
        return response.arrayBuffer()
      })
      .then((buffer) => {
        if (active) setData(buffer)
      })
      .catch((err: unknown) => {
        if (active) setLoadError(err instanceof Error ? err.message : 'PDF açılamadı.')
      })
    return () => {
      active = false
    }
  }, [url])

  useEffect(() => {
    const update = () => setWidth(Math.min(720, window.innerWidth - 48))
    update()
    window.addEventListener('resize', update)
    return () => window.removeEventListener('resize', update)
  }, [])

  return (
    <div className="book">
      <div className="book-bar">
        <div>
          <strong>{title}</strong>
          <small>{pages > 0 ? `${page} / ${pages}` : 'Hazırlanıyor'}</small>
        </div>
        <div className="book-actions">
          <button type="button" onClick={() => setPage((current) => Math.max(1, current - 1))} disabled={page <= 1}>Önceki</button>
          <button type="button" onClick={() => setPage((current) => Math.min(pages, current + 1))} disabled={page >= pages}>Sonraki</button>
          <a href={url} download={`${title}.pdf`}>İndir</a>
        </div>
      </div>

      <ul className="toc">
        {papers.map((paper) => (
          <li key={paper.id}>
            <button type="button" onClick={() => paper.sayfaBaslangic && setPage(paper.sayfaBaslangic)}>
              <span>{paper.baslik ?? paper.dosyaAdi}</span>
              <em>{paper.sayfaBaslangic ?? '—'}</em>
            </button>
          </li>
        ))}
      </ul>

      {loadError && <p className="error">{loadError}</p>}
      {data && (
        <Document file={{ data }} onLoadSuccess={({ numPages }) => { setPages(numPages); setPage(1) }} loading={<p className="status">Sayfalar açılıyor…</p>}>
          <Page pageNumber={page} width={width} renderTextLayer={false} renderAnnotationLayer={false} />
        </Document>
      )}
    </div>
  )
}
