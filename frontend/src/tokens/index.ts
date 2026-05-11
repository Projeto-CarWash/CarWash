import colors, { values as colorsValues, toCssVars as colorsToCssVars } from './colors'
import borderRadius, { values as borderRadiusValues, toCssVars as borderRadiusToCssVars } from './borderRadius'
import borderWidths, { values as borderWidthsValues, toCssVars as borderWidthsToCssVars } from './borderWidths'
import spacings, { values as spacingsValues, toCssVars as spacingsToCssVars } from './spacings'
import fontSizes, { values as fontSizesValues, toCssVars as fontSizesToCssVars } from './fontSizes'
import fontWeights, { values as fontWeightsValues, toCssVars as fontWeightsToCssVars } from './fontWeights'
import lineHeights, { values as lineHeightsValues, toCssVars as lineHeightsToCssVars } from './lineHeights'
import shadows, { values as shadowsValues, toCssVars as shadowsToCssVars } from './shadows'
import zIndex, { values as zIndexValues, toCssVars as zIndexToCssVars } from './zIndex'
import transitions, { values as transitionsValues, toCssVars as transitionsToCssVars } from './transitions'

export const allCssVars = () => ({
  ...colorsToCssVars(),
  ...borderRadiusToCssVars(),
  ...borderWidthsToCssVars(),
  ...spacingsToCssVars(),
  ...fontSizesToCssVars(),
  ...fontWeightsToCssVars(),
  ...lineHeightsToCssVars(),
  ...shadowsToCssVars(),
  ...zIndexToCssVars(),
  ...transitionsToCssVars(),
})

export {
  colors,
  borderRadius,
  borderWidths,
  spacings,
  fontSizes,
  fontWeights,
  lineHeights,
  shadows,
  zIndex,
  transitions,
  colorsValues,
  borderRadiusValues,
  borderWidthsValues,
  spacingsValues,
  fontSizesValues,
  fontWeightsValues,
  lineHeightsValues,
  shadowsValues,
  zIndexValues,
  transitionsValues,
}

export type { Colors } from './colors'
export type { BorderRadius } from './borderRadius'
export type { BorderWidths } from './borderWidths'
export type { Spacings } from './spacings'
export type { FontSizes } from './fontSizes'
export type { FontWeights } from './fontWeights'
export type { LineHeights } from './lineHeights'
export type { Shadows } from './shadows'
export type { ZIndex } from './zIndex'
export type { Transitions } from './transitions'

const tokens = {
  colors,
  borderRadius,
  borderWidths,
  spacings,
  fontSizes,
  fontWeights,
  lineHeights,
  shadows,
  zIndex,
  transitions,
}

export default tokens
