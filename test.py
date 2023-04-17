
num_tiles_x = num_tiles_y =32
num_tiles_z = 16



def PrintAABB(x, y, z) :
    min_point_index = z * (num_tiles_x + 1) * (num_tiles_y + 1) + y * (num_tiles_x + 1) + x
    max_point_index = (z + 1) * (num_tiles_x + 1) * (y + 1) + (num_tiles_y + 1) * (num_tiles_x + 1) + x + 1
    print(min_point_index, max_point_index)

PrintAABB(0, 0, 0)
PrintAABB(1, 0, 0)
PrintAABB(2, 0, 0)
PrintAABB(0, 0, 0)
PrintAABB(1, 0, 0)
PrintAABB(2, 0, 0)