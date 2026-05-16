export interface BordersType {
  radius: {
    none: number;
    sXXS: number;
    sXS: number;
    small: number;
    medium: number;
    large: number;
    xl: number;
    xxl: number;
    huge: number;
    giant: number;
  };
  width: {
    none: number;
    sXXS: number;
    sXS: number;
    small: number;
    medium: number;
  };
}

export const Borders: BordersType = {
  radius: {
    none: 0,
    sXXS: 4,
    sXS: 6,
    small: 8,
    medium: 15,
    large: 20,
    xl: 24,
    xxl: 27,
    huge: 30,
    giant: 40,
  },
  width: {
    none: 0,
    sXXS: 0.5,
    sXS: 1,
    small: 2,
    medium: 3,
  },
};
