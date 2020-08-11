import numpy as np


def doBoundingBoxesIntersect(a, b, c, d):
  """
  Check if bounding boxes do intersect. If one bounding box touches
  the other, they do intersect.
  First segment is of points a and b, second of c and d.
  """
  ll1_x = min(a[0], b[0])
  ll2_x = min(c[0], d[0])
  ll1_y = min(a[1], b[1])
  ll2_y = min(c[1], d[1])
  ur1_x = max(a[0], b[0])
  ur2_x = max(c[0], d[0])
  ur1_y = max(a[1], b[1])
  ur2_y = max(c[1], d[1])

  return ll1_x <= ur2_x and ur1_x >= ll2_x and ll1_y <= ur2_y and ur1_y >= ll2_y


def isPointOnLine(a, b, c):
  """
  Check if a point is on a line.
  """
  # move to origin
  aTmp = (0, 0)
  bTmp = (b[0] - a[0], b[1] - a[1])
  cTmp = (c[0] - a[0], c[1] - a[1])
  r = np.cross(bTmp, cTmp)
  return np.abs(r) < 0.0000000001


def isPointRightOfLine(a, b, c):
  """
  Check if a point (c) is right of a line (a-b).
  If (c) is on the line, it is not right it.
  """
  # move to origin
  aTmp = (0, 0)
  bTmp = (b[0] - a[0], b[1] - a[1])
  cTmp = (c[0] - a[0], c[1] - a[1])
  return np.cross(bTmp, cTmp) < 0


def lineSegmentTouchesOrCrossesLine(a, b, c, d):
  """
  Check if line segment (a-b) touches or crosses
  line segment (c-d).
  """
  return (
    isPointOnLine(a, b, c)
    or isPointOnLine(a, b, d)
    or (isPointRightOfLine(a, b, c) ^ isPointRightOfLine(a, b, d))
  )


def doLinesIntersect(a, b, c, d):
  """
  Check if line segments (a-b) and (c-d) intersect.
  """
  return (
    doBoundingBoxesIntersect(a, b, c, d)
    and lineSegmentTouchesOrCrossesLine(a, b, c, d)
    and lineSegmentTouchesOrCrossesLine(c, d, a, b)
  )
