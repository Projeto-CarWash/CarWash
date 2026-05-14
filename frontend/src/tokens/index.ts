import './borders/borders.css';
import './spacings/spacins.css';
import './colors/colors.css';
import './fontSizes/fontSizes.css';
import './fontWeights/fontWeights.css';
import './lineHeights/lineHeights.css';
import './shadows/shadows.css';
import './transitions/transitions.css';
import './zIndex/zIndex.css';

import type { BordersType } from './borders/index.ts';
import type { ColorsType } from './colors/index.ts';
import type { FontSizesType } from './fontSizes/index.ts';
import type { FontWeightsType } from './fontWeights/index.ts';
import type { LineHeightsType } from './lineHeights/index.ts';
import type { ShadowsType } from './shadows/index.ts';
import type { SpacingsType } from './spacings/index.ts';
import type { TransitionsType } from './transitions/index.ts';
import type { ZIndexType } from './zIndex/index.ts';

export { Borders } from './borders/index.ts';
export type { BordersType } from './borders/index.ts';

export { Spacings } from './spacings/index.ts';
export type { SpacingsType } from './spacings/index.ts';

export { Colors } from './colors/index.ts';
export type { ColorsType } from './colors/index.ts';

export { FontSizes } from './fontSizes/index.ts';
export type { FontSizesType } from './fontSizes/index.ts';

export { FontWeights } from './fontWeights/index.ts';
export type { FontWeightsType } from './fontWeights/index.ts';

export { LineHeights } from './lineHeights/index.ts';
export type { LineHeightsType } from './lineHeights/index.ts';

export { Shadows } from './shadows/index.ts';
export type { ShadowsType } from './shadows/index.ts';

export { Transitions } from './transitions/index.ts';
export type { TransitionsType } from './transitions/index.ts';

export { ZIndex } from './zIndex/index.ts';
export type { ZIndexType } from './zIndex/index.ts';

export interface AppThemeType {
  borders: BordersType;
  spacings: SpacingsType;
  colors: ColorsType;
  fontSizes: FontSizesType;
  fontWeights: FontWeightsType;
  lineHeights: LineHeightsType;
  shadows: ShadowsType;
  transitions: TransitionsType;
  zIndex: ZIndexType;
}
