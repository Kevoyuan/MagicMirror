import bpy

# Get the active object
obj = bpy.context.object

# Make a copy of the mesh data
obj.data = obj.data.copy()

# Add the decimate modifier to the object
mod = obj.modifiers.new(type='DECIMATE', name='Decimate')

# Set the decimate ratio
mod.ratio = 0.7

# Apply the modifier
bpy.ops.object.modifier_apply(modifier='Decimate')
