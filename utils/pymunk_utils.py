import pymunk
import numpy as np

from pymunk.vec2d import Vec2d
from scipy.spatial.transform import Rotation as R

# =============================================================================
# Initialization
# =============================================================================


def create_space(WIDTH, HEIGHT):
  space = pymunk.Space()
  space.iterations = 10
  space.sleep_time_threshold = 0.5

  static_body = space.static_body

  # Create segments around the edge of the screen.
  shape = pymunk.Segment(static_body, (1, 1), (1, HEIGHT), 1.0)
  space.add(shape)
  shape.elasticity = 1
  shape.friction = 1

  shape = pymunk.Segment(static_body, (WIDTH - 2, 1), (WIDTH - 2, HEIGHT), 1.0)
  space.add(shape)
  shape.elasticity = 1
  shape.friction = 1

  shape = pymunk.Segment(static_body, (1, 1 + 1), (WIDTH, 1 + 1), 1.0)
  space.add(shape)
  shape.elasticity = 1
  shape.friction = 1

  shape = pymunk.Segment(static_body, (1, HEIGHT), (WIDTH, HEIGHT), 1.0)
  space.add(shape)
  shape.elasticity = 1
  shape.friction = 1

  return space


# =============================================================================
# Physics
# =============================================================================


def limit_velocity(body, gravity, damping, dt):
  pymunk.Body.update_velocity(body, gravity, damping, dt)
  l = body.velocity.length
  if l > body.soldier.max_speed:
    scale = body.soldier.max_speed / l
    body.velocity = body.velocity * scale


def kill_lateral_velocity(b):
  # TODO : may be done in a better way

  # Later velocity
  b.angular_velocity = 0.0
  lat_dir = b.local_to_world(Vec2d(1, 0)) - b.position
  lat_vel = lat_dir.dot(b.velocity) * lat_dir
  imp = b.mass * -lat_vel
  b.apply_force_at_world_point(imp, b.position)

  # Behind velocity
  b.angular_velocity = 0.0
  lat_dir = b.local_to_world(Vec2d(0, 1)) - b.position
  lat_vel = lat_dir.dot(b.velocity) * lat_dir
  imp = b.mass * -lat_vel
  b.apply_force_at_world_point(imp, b.position)


def rotate_matrix(M, angle):
  assert type(M) == np.ndarray and M.shape[1] == 3, "M must be a np.array of Nx3"
  return R.from_euler("z", angle).apply(M)[:, :-1]


def spaced_vector(n, size, dist):
  dd = n * size + (n - 1) * dist - size
  return np.linspace(-dd / 2, dd / 2, n)


def calc_vertices(pos, h, w, angle):
  vs = []
  vs.append(list(Vec2d(h / 2, w / 2)) + [0])
  vs.append(list(Vec2d(h / 2, -w / 2)) + [0])
  vs.append(list(Vec2d(-h / 2, -w / 2)) + [0])
  vs.append(list(Vec2d(-h / 2, w / 2)) + [0])

  M = np.array(vs)
  MR = R.from_euler("z", angle).apply(M)[:, :-1].tolist()

  return [pos + Vec2d(v) for v in MR]


# =============================================================================
# Collisions
# =============================================================================


def begin_solve_enemy(arbiter, space, _):
  """
  When an enemy is in our melee range it is added.
  """
  s1, s2 = arbiter.shapes
  if s1.sensor and not s2.sensor:
    s1.body.soldier.enemy_melee_range.add(s2.body.soldier)
  elif s2.sensor and not s1.sensor:
    s2.body.soldier.enemy_melee_range.add(s1.body.soldier)
  return True


def separate_solve_enemy(arbiter, space, _):
  """
  When an enemy leaves in our melee range it is removed.
  """
  s1, s2 = arbiter.shapes
  if s1.sensor and not s2.sensor:
    s1.body.soldier.enemy_melee_range.remove(s2.body.soldier)
  elif s2.sensor and not s1.sensor:
    s2.body.soldier.enemy_melee_range.remove(s1.body.soldier)
  return True


def add_collisions(space):
  CH_33 = space.add_collision_handler(3, 3)  # 3 is the reference value for
  CH_32 = space.add_collision_handler(3, 2)  # utility objects
  CH_31 = space.add_collision_handler(3, 1)
  CH_12 = space.add_collision_handler(2, 1)

  # Collisions between soldiers and soldiers' sensors
  CH_12.begin = begin_solve_enemy
  CH_12.separate = separate_solve_enemy

  # Utility objects should not create collisions
  CH_33.begin = lambda *args, **kwargs: False
  CH_32.begin = lambda *args, **kwargs: False
  CH_31.begin = lambda *args, **kwargs: False


def do_polygons_intersect(a, b):
  """
  
  Parameters
  ----------
  a : TYPE
    list of vertices.
  b : TYPE
    list of vertices.
  Returns
  -------
  True is intersects False otherwise
  """

  polygons = [a, b]
  minA, maxA, projected, i, i1, j, minB, maxB = (
    None,
    None,
    None,
    None,
    None,
    None,
    None,
    None,
  )

  for i in range(len(polygons)):

    # for each polygon, look at each edge of the polygon, and determine if it separates
    # the two shapes
    polygon = polygons[i]
    for i1 in range(len(polygon)):

      # grab 2 vertices to create an edge
      i2 = (i1 + 1) % len(polygon)
      p1 = polygon[i1]
      p2 = polygon[i2]

      # find the line perpendicular to this edge
      normal = {"x": p2[1] - p1[1], "y": p1[0] - p2[0]}

      minA, maxA = None, None
      # for each vertex in the first shape, project it onto the line perpendicular to the edge
      # and keep track of the min and max of these values
      for j in range(len(a)):
        projected = normal["x"] * a[j][0] + normal["y"] * a[j][1]
        if (minA is None) or (projected < minA):
          minA = projected

        if (maxA is None) or (projected > maxA):
          maxA = projected

      # for each vertex in the second shape, project it onto the line perpendicular to the edge
      # and keep track of the min and max of these values
      minB, maxB = None, None
      for j in range(len(b)):
        projected = normal["x"] * b[j][0] + normal["y"] * b[j][1]
        if (minB is None) or (projected < minB):
          minB = projected

        if (maxB is None) or (projected > maxB):
          maxB = projected

      # if there is no overlap between the projects, the edge we are looking at separates the two
      # polygons, and we know there is no overlap
      if (maxA < minB) or (maxB < minA):
        return False
  return True
