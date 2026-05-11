const values = {
  hide: '-1',
  base: '0',
  raised: '10',
  dropdown: '100',
  sticky: '200',
  overlay: '300',
  modal: '400',
  popover: '500',
  toast: '600',
  tooltip: '700',
} as const

const vars = {
  hide: 'var(--zIndex--hide)',
  base: 'var(--zIndex--base)',
  raised: 'var(--zIndex--raised)',
  dropdown: 'var(--zIndex--dropdown)',
  sticky: 'var(--zIndex--sticky)',
  overlay: 'var(--zIndex--overlay)',
  modal: 'var(--zIndex--modal)',
  popover: 'var(--zIndex--popover)',
  toast: 'var(--zIndex--toast)',
  tooltip: 'var(--zIndex--tooltip)',
} as const

const toCssVars = () => ({
  '--zIndex--hide': values.hide,
  '--zIndex--base': values.base,
  '--zIndex--raised': values.raised,
  '--zIndex--dropdown': values.dropdown,
  '--zIndex--sticky': values.sticky,
  '--zIndex--overlay': values.overlay,
  '--zIndex--modal': values.modal,
  '--zIndex--popover': values.popover,
  '--zIndex--toast': values.toast,
  '--zIndex--tooltip': values.tooltip,
})

export type ZIndex = typeof vars
export { values, toCssVars }
export default vars
