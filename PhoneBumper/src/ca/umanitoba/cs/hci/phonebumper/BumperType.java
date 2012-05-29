package ca.umanitoba.cs.hci.phonebumper;

public enum BumperType
{
  Color,
  Gray,
  Depth,
  DepthMaskOnColor,
  HighDepth,
  HighDepthColumns,
  AvgHighDepthColumns,
  GrayDepth, //-- These five are the new ones
  ClosestObjectRange,
  MaskOnColor,
  MaskOnGray,
  Iconic,
  Black,
  //HighDepth but not just white but in a color or gray gradient
  //ClosestObjectRange but not just white but in a color or gray gradient
  //ClosestObjectRangeMask both in color and in gray
  None
};