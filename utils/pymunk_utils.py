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
