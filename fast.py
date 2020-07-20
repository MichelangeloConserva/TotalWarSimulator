import pymunk
space = pymunk.Space()
space.gravity = 0, 10
body = pymunk.Body(1,2)
space.add(body)
def zero_gravity(body, gravity, damping, dt):
    pymunk.Body.update_velocity(body, (0,0), damping, dt)
body.velocity_func = zero_gravity
space.step(1)
space.step(1)
print(body.position, body.velocity)



