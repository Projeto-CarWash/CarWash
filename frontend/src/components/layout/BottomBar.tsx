export function BottomBar() {
  const items = [
    { type: 'status', text: 'ONLINE', online: true },
    { type: 'box', text: 'BOX 01' },
    { type: 'car', text: 'VW GOLF GTI' },
    { type: 'time', text: '00:42:18' },
    { type: 'box', text: 'BOX 02' },
    { type: 'car', text: 'HONDA CIVIC' },
    { type: 'time', text: '00:12:04' },
    { type: 'box', text: 'BOX 03' },
    { type: 'car', text: 'BMW M3' },
    { type: 'time', text: '01:14:32' },
    { type: 'box', text: 'BOX 04' },
    { type: 'car', text: 'FIAT ARGO' },
    { type: 'time', text: '00:34:22' },
    { type: 'telemetry', text: 'TEMP. ÁGUA 28°C' },
    { type: 'telemetry', text: 'PRESSÃO 140 PSI' },
  ];

  return (
    <footer className="fixed bottom-0 left-64 right-0 z-50 flex h-7 items-center border-t border-zinc-800/60 bg-zinc-950 px-4 font-mono text-[10px] tracking-wider text-zinc-500">
      <div className="flex flex-1 items-center gap-0 overflow-hidden">
        {items.map((item, index) => (
          <div key={index} className="flex shrink-0 items-center">
            {index > 0 && (
              <span className="mx-2 text-red-600/70 select-none">/</span>
            )}
            {item.type === 'status' && (
              <span className="flex items-center gap-1.5">
                <span className="inline-block h-1.5 w-1.5 rounded-full bg-green-500 shadow-[0_0_6px_rgba(34,197,94,0.6)]" />
                <span className="font-semibold text-green-500">{item.text}</span>
              </span>
            )}
            {item.type === 'box' && (
              <span className="font-semibold text-zinc-400">{item.text}</span>
            )}
            {item.type === 'car' && (
              <span className="text-zinc-500">{item.text}</span>
            )}
            {item.type === 'time' && (
              <span className="text-zinc-600">{item.text}</span>
            )}
            {item.type === 'telemetry' && (
              <span className="text-zinc-500">{item.text}</span>
            )}
          </div>
        ))}
      </div>
      <div className="shrink-0 border-l border-zinc-800 pl-3">
        <span className="text-zinc-600">VERSÃO v2.4.1</span>
      </div>
    </footer>
  );
}
