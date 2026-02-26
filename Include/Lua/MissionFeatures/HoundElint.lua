
do
    if STTS ~= nil and STTS.DIRECTORY == "C:\\Users\\Ciaran\\Dropbox\\Dev\\DCS\\DCS-SRS\\install-build" then
        STTS.DIRECTORY = "C:\\Program Files\\DCS-SimpleRadio-Standalone\\ExternalAudio"
    end

    math.random(math.ceil(timer.getTime0()+timer.getTime()))
    for i=1,math.random(2,5) do
        math.random(math.random(math.floor(math.random()*300),300),math.random(math.floor(math.random()*10000),10000))
    end
end

do
    HOUND = {
        VERSION = "0.5.0",
        DEBUG = false,
        ELLIPSE_PERCENTILE = 0.6,
        DATAPOINTS_NUM = 30,
        DATAPOINTS_INTERVAL = 30,
        CONTACT_TIMEOUT = 900,
        MAX_ANGULAR_RES_DEG = 20,
        ANTENNA_FACTOR = 1.0,
        MGRS_PRECISION = 5,
        EXTENDED_INFO = true,
        USE_LEGACY_MARKERS = true,
        MARKER_MIN_ALPHA = 0.05,
        MARKER_MAX_ALPHA = 0.2,
        MARKER_LINE_OPACITY = 0.3,
        MARKER_TEXT_POINTER = "⇙ ", -- "¤ « "
        TTS_ENGINE = {'HOUND','STTS'},
        MENU_PAGE_LENGTH = 9,
        REF_DIST = 75000, -- Do not change, used for datapoint weighting
        ENABLE_KALMAN = true,
        KALMAN_DEBUG = false,
        AUTO_ADD_PLATFORM_BY_PAYLOAD = true, -- if true, will automatically add platforms that have ELINT payloads (currently, due to DCS limits, only works for units spawning with the required pods)
    }

    HOUND.MARKER = {
        NONE = 0,
        SITE_ONLY = 1,
        POINT = 2,
        CIRCLE = 3,
        DIAMOND = 4,
        OCTAGON = 5,
        POLYGON = 6
    }

    HOUND.EVENTS = {
        NO_CHANGE     = 0,
        HOUND_ENABLED = 1,
        HOUND_DISABLED = 2,
        PLATFORM_ADDED = 3,
        PLATFORM_REMOVED = 4,
        PLATFORM_DESTROYED = 5,
        TRANSMITTER_ADDED = 6,
        TRANSMITTER_REMOVED = 7,
        TRANSMITTER_DESTROYED = 8,
        RADAR_NEW = 9,
        RADAR_DETECTED = 10,
        RADAR_UPDATED = 11,
        RADAR_DESTROYED = 12,
        RADAR_ALIVE = 13,
        RADAR_ASLEEP = 14,
        SITE_NEW = 15,
        SITE_CREATED = 16,
        SITE_UPDATED = 17,
        SITE_CLASSIFIED = 18,
        SITE_REMOVED = 19,
        SITE_ALIVE = 20,
        SITE_ASLEEP = 21,
        SITE_LAUNCH = 22,
    }

    HOUND.INSTANCES = {}

    function HOUND.getInstance(InstanceId)
        if HOUND.setContains(HOUND.INSTANCES,InstanceId) then
            return HOUND.INSTANCES[InstanceId]
        end
        return nil
    end

    function HOUND.setMgrsPresicion(value)
        if type(value) == "number" then
            HOUND.MGRS_PRECISION = math.min(1,math.max(5,math.floor(value)))
        end
    end

    function HOUND.showExtendedInfo(value)
        if type(value) == "boolean" then
            HOUND.EXTENDED_INFO = value
        end
    end

    function HOUND.addEventHandler(handler)
        HOUND.EventHandler.addEventHandler(handler)
    end

    function HOUND.removeEventHandler(handler)
        HOUND.EventHandler.removeEventHandler(handler)
    end

    HOUND.Contact = {}
    HOUND.Comms = {}

    function HOUND.inheritsFrom( baseClass )

        local new_class = {}
        local class_mt = { __index = new_class }

        function new_class:create()
            local newinst = {}
            setmetatable( newinst, class_mt )
            return newinst
        end

        if nil ~= baseClass then
            setmetatable( new_class, { __index = baseClass } )
        end

        function new_class:class()
            return new_class
        end

        function new_class:superClass()
            return baseClass
        end

        function new_class:isa( theClass )
            local b_isa = false
            local cur_class = new_class

            while ( nil ~= cur_class ) and ( false == b_isa ) do
                if cur_class == theClass then
                    b_isa = true
                else
                    cur_class = cur_class:superClass()
                end
            end
            return b_isa
        end
        return new_class
    end

    function HOUND.Length(T)
        local count = 0
        if T ~= nil then for _ in pairs(T) do count = count + 1 end end
        return count
    end

    function HOUND.setContains(set, key)
        if not set or not key then return false end
        return set[key] ~= nil
    end

    function HOUND.setContainsValue(set,value)
        if not set or not value then return false end
        for _,v in pairs(set) do
            if v == value then
                return true
            end
        end
        return false
    end

    function HOUND.setIntersection(a,b)
        local res = {}
        for k in pairs(a) do
          res[k] = b[k]
        end
        return res
      end

    function HOUND.Gaussian(mean, sigma)
        return math.sqrt(-2 * math.log(math.random())) * sigma *
                   math.cos(2 * math.pi * math.random()) + mean
    end

    function HOUND.Clamp(value, min, max)
        return math.max(min, math.min(max, value))
    end

    function HOUND.MixedGaussian(mean, sigma, uniform)
        return mean + sigma * (math.random() - 0.5) * uniform + HOUND.Gaussian(0, sigma) * (1 - uniform)
    end

    function HOUND.reverseLookup(tbl,value)
        if type(tbl) ~= "table" or type(value) == "nil" then return end
        for k,v in pairs(tbl) do
            if v == value then return k end
        end
    end

    function string.split(str, delim)
        if not str or type(str) ~= "string" then return {str} end
        if not delim then
            delim = "%S"
        end
        local chunks = {}
        for substring in str:gmatch("[^" .. delim .. "]+") do
            table.insert(chunks, substring)
        end
        return chunks
    end
end
do
    local l_env = env

    HOUND.Logger = {
        level = 3
    }
    HOUND.Logger.__index = HOUND.Logger

    HOUND.Logger.LEVEL = {
        ["error"]=1,
        ["warning"]=2,
        ["info"]=3,
        ["debug"]=4,
        ["trace"]=5,
    }

    function HOUND.Logger.setBaseLevel(level)
        if HOUND.setContainsValue(HOUND.Logger.LEVEL,level) then
            HOUND.Logger.level = level
        end
    end

    function HOUND.Logger.formatText(text, ...)
        if not text then
            return ""
        end
        if type(text) ~= 'string' then
            text = tostring(text)
        else
            if arg and arg.n and arg.n > 0 then
                local pArgs = {}
                for index,value in ipairs(arg) do
                    pArgs[index] = tostring(value)
                end
                text = text:format(unpack(pArgs))
            end
        end
        local fName = nil
        local cLine = nil
        if debug then
            local dInfo = debug.getinfo(3)
            fName = dInfo.name
            cLine = dInfo.currentline
        end
        if fName and cLine then
            return fName .. '|' .. cLine .. ': ' .. text
        elseif cLine then
            return cLine .. ': ' .. text
        else
            return ' ' .. text
        end
    end

    function HOUND.Logger.print(level, text)
        local texts = {text}
        local levelChar = 'E'
        local logFunction = l_env.error
        if level == HOUND.Logger.LEVEL["warning"] then
            levelChar = 'W'
            logFunction = l_env.warning
        elseif level == HOUND.Logger.LEVEL["info"] then
            levelChar = 'I'
            logFunction = l_env.info
        elseif level == HOUND.Logger.LEVEL["debug"] then
            levelChar = 'D'
            logFunction = l_env.info
        elseif level == HOUND.Logger.LEVEL["trace"] then
            levelChar = 'T'
            logFunction = l_env.info
        end
        for i = 1, #texts do
            if i == 1 then
                logFunction('[Hound](' .. levelChar.. ') - ' .. texts[i])
            else
                logFunction(texts[i])
            end
        end
    end

    function HOUND.Logger.error(text, ...)
        if HOUND.Logger.level >= 1 then
            text = HOUND.Logger.formatText(text, unpack(arg))
            HOUND.Logger.print(1, text)
        end
    end

    function HOUND.Logger.warn(text, ...)
        if HOUND.Logger.level >= 2 then
            text = HOUND.Logger.formatText(text, unpack(arg))
            HOUND.Logger.print(2, text)
        end
    end

    function HOUND.Logger.info(text, ...)
        if HOUND.Logger.level >= 3 then
            text = HOUND.Logger.formatText(text, unpack(arg))
            HOUND.Logger.print(3, text)
        end
    end

    function HOUND.Logger.debug(text, ...)
        if HOUND.Logger.level >= 4 then
            text = HOUND.Logger.formatText(text, unpack(arg))
            HOUND.Logger.print(4, text)
        end
    end

    function HOUND.Logger.trace(text, ...)
        if HOUND.Logger.level >= 5 then
            text = HOUND.Logger.formatText(text, unpack(arg))
            HOUND.Logger.print(5, text)
        end
    end

    function HOUND.Logger.onScreenDebug(text,time)
        if type(text) ~= "string" then return end
        if type(time) ~= "number" then
            time = 15
        end
        trigger.action.outText(text,math.ceil(time))
    end

    if HOUND.DEBUG then
        HOUND.Logger.setBaseLevel(HOUND.Logger.LEVEL.trace)
    end
end
do
    local l_math = math
    HOUND.Mist = {}
    HOUND.Mist.__index = HOUND.Mist

    function HOUND.Mist.getNorthCorrection(gPoint)	--gets the correction needed for true north
		local point = HOUND.Mist.utils.deepCopy(gPoint)
		if not point.z then --Vec2; convert to Vec3
			point.z = point.y
			point.y = 0
		end
		local lat, lon = coord.LOtoLL(point)
		local north_posit = coord.LLtoLO(lat + 1, lon)
		return l_math.atan2(north_posit.z - point.z, north_posit.x - point.x)
	end

function HOUND.Mist.getAvgPoint(points)
	local avgX, avgY, avgZ, totNum = 0, 0, 0, 0
	for i = 1, #points do
        local nPoint = HOUND.Mist.utils.makeVec3(points[i])
		if nPoint.z then
			avgX = avgX + nPoint.x
			avgY = avgY + nPoint.y
			avgZ = avgZ + nPoint.z
			totNum = totNum + 1
		end
	end
	if totNum ~= 0 then
		return {x = avgX/totNum, y = avgY/totNum, z = avgZ/totNum}
	end
end

function HOUND.Mist.getAvgPos(unitNames)
	local avgX, avgY, avgZ, totNum = 0, 0, 0, 0
	for i = 1, #unitNames do
		local unit
		if Unit.getByName(unitNames[i]) then
			unit = Unit.getByName(unitNames[i])
		elseif StaticObject.getByName(unitNames[i]) then
			unit = StaticObject.getByName(unitNames[i])
		end
		if unit and unit:isExist() == true then
			local pos = unit:getPosition().p
			if pos then -- you never know O.o
				avgX = avgX + pos.x
				avgY = avgY + pos.y
				avgZ = avgZ + pos.z
				totNum = totNum + 1
			end
		end
	end
	if totNum ~= 0 then
		return {x = avgX/totNum, y = avgY/totNum, z = avgZ/totNum}
	end
end

function HOUND.Mist.getAvgGroupPos(groupName)
	if type(groupName) == 'string' and Group.getByName(groupName) and Group.getByName(groupName):isExist() == true then
		groupName = Group.getByName(groupName)
	end
	local units = {}
	for i = 1, groupName:getSize() do
		table.insert(units, groupName:getUnit(i):getName())
	end

	return HOUND.Mist.getAvgPos(units)

end

end

do -- HOUND.Mist.vec scope
	HOUND.Mist.vec = {}

	function HOUND.Mist.vec.add(vec1, vec2)
		return {x = vec1.x + vec2.x, y = vec1.y + vec2.y, z = vec1.z + vec2.z}
	end

	function HOUND.Mist.vec.sub(vec1, vec2)
		return {x = vec1.x - vec2.x, y = vec1.y - vec2.y, z = vec1.z - vec2.z}
	end

	function HOUND.Mist.vec.scalarMult(vec, mult)
		return {x = vec.x*mult, y = vec.y*mult, z = vec.z*mult}
	end

	HOUND.Mist.vec.scalar_mult = HOUND.Mist.vec.scalarMult

	function HOUND.Mist.vec.dp (vec1, vec2)
		return vec1.x*vec2.x + vec1.y*vec2.y + vec1.z*vec2.z
	end

	function HOUND.Mist.vec.cp(vec1, vec2)
		return { x = vec1.y*vec2.z - vec1.z*vec2.y, y = vec1.z*vec2.x - vec1.x*vec2.z, z = vec1.x*vec2.y - vec1.y*vec2.x}
	end

	function HOUND.Mist.vec.mag(vec)
		return (vec.x^2 + vec.y^2 + vec.z^2)^0.5
	end

	function HOUND.Mist.vec.getUnitVec(vec)
		local mag = HOUND.Mist.vec.mag(vec)
		return { x = vec.x/mag, y = vec.y/mag, z = vec.z/mag }
	end

	function HOUND.Mist.vec.rotateVec2(vec2, theta)
		return { x = vec2.x*math.cos(theta) - vec2.y*math.sin(theta), y = vec2.x*math.sin(theta) + vec2.y*math.cos(theta)}
	end

    function HOUND.Mist.vec.normalize(vec3)
        local mag =  HOUND.Mist.vec.mag(vec3)
        if mag ~= 0 then
            return HOUND.Mist.vec.scalar_mult(vec3, 1.0 / mag)
        end
    end
end

do -- HOUND.Mist.util scope
 	HOUND.Mist.utils = {}

	function HOUND.Mist.utils.toDegree(angle)
		return angle*180/math.pi
	end

	function HOUND.Mist.utils.toRadian(angle)
		return angle*math.pi/180
	end

	function HOUND.Mist.utils.metersToNM(meters)
		return meters/1852
	end

	function HOUND.Mist.utils.metersToFeet(meters)
		return meters/0.3048
	end

	function HOUND.Mist.utils.NMToMeters(nm)
		return nm*1852
	end

	function HOUND.Mist.utils.feetToMeters(feet)
		return feet*0.3048
	end

	function HOUND.Mist.utils.hexToRGB(hex, l) -- because for some reason the draw tools use hex when everything is rgba 0 - 1
        local int = 255
        if l then
         int = 1
        end
        if hex and type(hex) == 'string' then
            local val = {}
            hex = string.gsub(hex, '0x', '')
            if string.len(hex) == 8 then
                val[1] = tonumber("0x"..hex:sub(1,2)) / int
                val[2] = tonumber("0x"..hex:sub(3,4)) / int
                val[3] = tonumber("0x"..hex:sub(5,6)) / int
                val[4] = tonumber("0x"..hex:sub(7,8)) / int

                return val
            end
        end
   end

	function HOUND.Mist.utils.makeVec2(vec)
		if vec.z then
			return {x = vec.x, y = vec.z}
		else
			return {x = vec.x, y = vec.y}	-- it was actually already vec2.
		end
	end

	function HOUND.Mist.utils.makeVec3(vec, y)
		if not vec.z then
			if vec.alt and not y then
				y = vec.alt
			elseif not y then
				y = 0
			end
			return {x = vec.x, y = y, z = vec.y}
		else
			return {x = vec.x, y = vec.y, z = vec.z}	-- it was already Vec3, actually.
		end
	end

	function HOUND.Mist.utils.makeVec3GL(vec, offset)
		local adj = offset or 0

		if not vec.z then
			return {x = vec.x, y = (land.getHeight(vec) + adj), z = vec.y}
		else
			return {x = vec.x, y = (land.getHeight({x = vec.x, y = vec.z}) + adj), z = vec.z}
		end
	end

    function HOUND.Mist.utils.getHeadingPoints(point1, point2, north) -- sick of writing this out.
        if north then
			local p1 = HOUND.Mist.utils.get3DDist(point1)
            return HOUND.Mist.utils.getDir(HOUND.Mist.vec.sub(HOUND.Mist.utils.makeVec3(point2), p1), p1)
        else
            return HOUND.Mist.utils.getDir(HOUND.Mist.vec.sub(HOUND.Mist.utils.makeVec3(point2), HOUND.Mist.utils.makeVec3(point1)))
        end
    end

	function HOUND.Mist.utils.getDir(vec, point)
		local dir = math.atan2(vec.z, vec.x)
		if point then
			dir = dir + HOUND.Mist.getNorthCorrection(point)
		end
		if dir < 0 then
			dir = dir + 2 * math.pi	-- put dir in range of 0 to 2*pi
		end
		return dir
	end

	function HOUND.Mist.utils.get2DDist(point1, point2)
        if not point1 then
            log:warn("HOUND.Mist.utils.get2DDist  1st input value is nil")
        end
        if not point2 then
            log:warn("HOUND.Mist.utils.get2DDist  2nd input value is nil")
        end
		point1 = HOUND.Mist.utils.makeVec3(point1)
		point2 = HOUND.Mist.utils.makeVec3(point2)
		return HOUND.Mist.vec.mag({x = point1.x - point2.x, y = 0, z = point1.z - point2.z})
	end

	function HOUND.Mist.utils.get3DDist(point1, point2)
        if not point1 then
            log:warn("HOUND.Mist.utils.get2DDist  1st input value is nil")
        end
        if not point2 then
            log:warn("HOUND.Mist.utils.get2DDist  2nd input value is nil")
        end
		return HOUND.Mist.vec.mag({x = point1.x - point2.x, y = point1.y - point2.y, z = point1.z - point2.z})
	end

	function HOUND.Mist.utils.deepCopy(object)
		local lookup_table = {}
		local function _copy(object)
			if type(object) ~= "table" then
				return object
			elseif lookup_table[object] then
				return lookup_table[object]
			end
			local new_table = {}
			lookup_table[object] = new_table
			for index, value in pairs(object) do
				new_table[_copy(index)] = _copy(value)
			end
			return setmetatable(new_table, getmetatable(object))
		end
		return _copy(object)
	end

	function HOUND.Mist.utils.round(num, idp)
		local mult = 10^(idp or 0)
		return math.floor(num * mult + 0.5) / mult
	end

function HOUND.Mist.utils.basicSerialize(var)
    if var == nil then
        return "\"\""
    else
        if ((type(var) == 'number') or
                (type(var) == 'boolean') or
                (type(var) == 'function') or
                (type(var) == 'table') or
                (type(var) == 'userdata') ) then
                    return tostring(var)
        elseif type(var) == 'string' then
            var = string.format('%q', var)
            return var
        end
    end
end

function HOUND.Mist.utils.tableShowSorted(tbls, v)
	local vars = v or {}
	local loc = vars.loc or ""
	local indent = vars.indent or ""
	local tableshow_tbls = vars.tableshow_tbls or {}
	local tbl = tbls or {}

	if type(tbl) == 'table' then --function only works for tables!
		tableshow_tbls[tbl] = loc

		local tbl_str = {}

		tbl_str[#tbl_str + 1] = indent .. '{\n'

		local sorted = {}
		local function byteCompare(str1, str2)
			local shorter = string.len(str1)
			if shorter > string.len(str2) then
				 shorter = string.len(str2)
			end
			for i = 1, shorter do
				local b1 = string.byte(str1, i)
				local b2 = string.byte(str2, i)

				if b1 < b2 then
					return true
				elseif b1 > b2 then
					return false
				end

			end
			return false
		end
		for ind, val in pairs(tbl) do -- serialize its fields
			local indS = tostring(ind)
			local ins = {ind = indS, val = val}
			local index
			if #sorted > 0 then
				local found = false
				for i = 1, #sorted do
					if byteCompare(indS, tostring(sorted[i].ind)) == true then
						index = i
						break
					end
				end
			end
			if index then
				table.insert(sorted, index, ins)
			else
				table.insert(sorted, ins)
			end
		end
		for i = 1, #sorted do
			local ind = sorted[i].ind
			local val = sorted[i].val

			if type(ind) == "number" then
				tbl_str[#tbl_str + 1] = indent
				tbl_str[#tbl_str + 1] = loc .. '['
				tbl_str[#tbl_str + 1] = tostring(ind)
				tbl_str[#tbl_str + 1] = '] = '
			else
				tbl_str[#tbl_str + 1] = indent
				tbl_str[#tbl_str + 1] = loc .. '['
				tbl_str[#tbl_str + 1] = HOUND.Mist.utils.basicSerialize(ind)
				tbl_str[#tbl_str + 1] = '] = '
			end

			if ((type(val) == 'number') or (type(val) == 'boolean')) then
				tbl_str[#tbl_str + 1] = tostring(val)
				tbl_str[#tbl_str + 1] = ',\n'
			elseif type(val) == 'string' then
				tbl_str[#tbl_str + 1] = HOUND.Mist.utils.basicSerialize(val)
				tbl_str[#tbl_str + 1] = ',\n'
			elseif type(val) == 'nil' then -- won't ever happen, right?
				tbl_str[#tbl_str + 1] = 'nil,\n'
			elseif type(val) == 'table' then
				if tableshow_tbls[val] then
					tbl_str[#tbl_str + 1] = ' already defined: ' .. tableshow_tbls[val] .. ',\n'
				else
					tableshow_tbls[val] = loc .. '["' .. ind .. '"]'
					tbl_str[#tbl_str + 1] = HOUND.Mist.utils.tableShowSorted(val, {loc =  loc .. '["' .. ind .. '"]', indent = indent .. '    ', tableshow_tbls = tableshow_tbls})
					tbl_str[#tbl_str + 1] = ',\n'
				end
			elseif type(val) == 'function' then
				if debug and debug.getinfo then
					local fcnname = tostring(val)
					local info = debug.getinfo(val, "S")
					if info.what == "C" then
						tbl_str[#tbl_str + 1] =  ', C function\n'
					else
						if (string.sub(info.source, 1, 2) == [[./]]) then
							tbl_str[#tbl_str + 1] = string.format('%q',  'function, defined in (' ..  '-' .. info.lastlinedefined .. ')' .. info.source) ..',\n'
						else
							tbl_str[#tbl_str + 1] = string.format('%q', 'function, defined in (' ..  '-' .. info.lastlinedefined .. ')') ..',\n'
						end
					end

				else
					tbl_str[#tbl_str + 1] = 'a function,\n'
				end
			else
				tbl_str[#tbl_str + 1] = 'unable to serialize value type ' .. HOUND.Mist.utils.basicSerialize(type(val)) .. ' at index ' .. tostring(ind)
			end
		end

		tbl_str[#tbl_str + 1] = indent .. '}'
		return table.concat(tbl_str)
	end

end

function HOUND.Mist.utils.tableShow(tbl, loc, indent, tableshow_tbls) --based on serialize_slmod, this is a _G serialization
	tableshow_tbls = tableshow_tbls or {} --create table of tables
	loc = loc or ""
	indent = indent or ""
	if type(tbl) == 'table' then --function only works for tables!
		tableshow_tbls[tbl] = loc

		local tbl_str = {}

		tbl_str[#tbl_str + 1] = indent .. '{\n'

		for ind, val in pairs(tbl) do
			if type(ind) == "number" then
				tbl_str[#tbl_str + 1] = indent
				tbl_str[#tbl_str + 1] = loc .. '['
				tbl_str[#tbl_str + 1] = tostring(ind)
				tbl_str[#tbl_str + 1] = '] = '
			else
				tbl_str[#tbl_str + 1] = indent
				tbl_str[#tbl_str + 1] = loc .. '['
				tbl_str[#tbl_str + 1] = HOUND.Mist.utils.basicSerialize(ind)
				tbl_str[#tbl_str + 1] = '] = '
			end

			if ((type(val) == 'number') or (type(val) == 'boolean')) then
				tbl_str[#tbl_str + 1] = tostring(val)
				tbl_str[#tbl_str + 1] = ',\n'
			elseif type(val) == 'string' then
				tbl_str[#tbl_str + 1] = HOUND.Mist.utils.basicSerialize(val)
				tbl_str[#tbl_str + 1] = ',\n'
			elseif type(val) == 'nil' then -- won't ever happen, right?
				tbl_str[#tbl_str + 1] = 'nil,\n'
			elseif type(val) == 'table' then
				if tableshow_tbls[val] then
					tbl_str[#tbl_str + 1] = tostring(val) .. ' already defined: ' .. tableshow_tbls[val] .. ',\n'
				else
					tableshow_tbls[val] = loc ..	'[' .. HOUND.Mist.utils.basicSerialize(ind) .. ']'
					tbl_str[#tbl_str + 1] = tostring(val) .. ' '
					tbl_str[#tbl_str + 1] = HOUND.Mist.utils.tableShow(val,	loc .. '[' .. HOUND.Mist.utils.basicSerialize(ind).. ']', indent .. '    ', tableshow_tbls)
					tbl_str[#tbl_str + 1] = ',\n'
				end
			elseif type(val) == 'function' then
				if debug and debug.getinfo then
					local fcnname = tostring(val)
					local info = debug.getinfo(val, "S")
					if info.what == "C" then
						tbl_str[#tbl_str + 1] = string.format('%q', fcnname .. ', C function') .. ',\n'
					else
						if (string.sub(info.source, 1, 2) == [[./]]) then
							tbl_str[#tbl_str + 1] = string.format('%q', fcnname .. ', defined in (' .. info.linedefined .. '-' .. info.lastlinedefined .. ')' .. info.source) ..',\n'
						else
							tbl_str[#tbl_str + 1] = string.format('%q', fcnname .. ', defined in (' .. info.linedefined .. '-' .. info.lastlinedefined .. ')') ..',\n'
						end
					end

				else
					tbl_str[#tbl_str + 1] = 'a function,\n'
				end
			else
				tbl_str[#tbl_str + 1] = 'unable to serialize value type ' .. HOUND.Mist.utils.basicSerialize(type(val)) .. ' at index ' .. tostring(ind)
			end
		end

		tbl_str[#tbl_str + 1] = indent .. '}'
		return table.concat(tbl_str)
	end
end

do
    HOUND.Mist.shape = {}
    function HOUND.Mist.shape.insideShape(shape1, shape2, full)
        if shape1.radius then -- probably a circle
            if shape2.radius then
                 return HOUND.Mist.shape.circleInCircle(shape1, shape2, full)
            elseif shape2[1] then
                 return HOUND.Mist.shape.circleInPoly(shape1, shape2, full)
            end

        elseif shape1[1] then -- shape1 is probably a polygon
            if shape2.radius then
                return  HOUND.Mist.shape.polyInCircle(shape1, shape2, full)
            elseif shape2[1] then
                return  HOUND.Mist.shape.polyInPoly(shape1, shape2, full)
            end
        end
        return false
    end

    function HOUND.Mist.shape.circleInCircle(c1, c2, full)
        if not full then -- quick partial check
            if HOUND.Mist.utils.get2DDist(c1.point, c2.point) <= c2.radius then
                return true
            end
        end
        local theta = HOUND.Mist.utils.getHeadingPoints(c2.point, c1.point) -- heading from
        if full then
            return  HOUND.Mist.utils.get2DDist(HOUND.Mist.projectPoint(c1.point, c1.radius, theta), c2.point) <= c2.radius
        else
            return HOUND.Mist.utils.get2DDist(HOUND.Mist.projectPoint(c1.point, c1.radius, theta + math.pi), c2.point) <= c2.radius
        end
        return false
    end
    function HOUND.Mist.shape.circleInPoly(circle, poly, full)

        if poly and type(poly) == 'table' and circle and type(circle) == 'table' and circle.radius and circle.point then
            if not full then
                for i = 1, #poly do
                    if HOUND.Mist.utils.get2DDist(circle.point, poly[i]) <= circle.radius then
                        return true
                    end
                end
            end
            local count = 0
            for i = 1, #poly do
                local theta -- heading of each set of points
                if i == #poly then
                    theta = HOUND.Mist.utils.getHeadingPoints(poly[i],poly[1])
                else
                    theta = HOUND.Mist.utils.getHeadingPoints(poly[i],poly[i+1])
                end
                local pPoint = HOUND.Mist.projectPoint(circle.point, circle.radius, theta - (math.pi/180))
                local oPoint = HOUND.Mist.projectPoint(circle.point, circle.radius, theta + (math.pi/180))
                if HOUND.Mist.pointInPolygon(pPoint, poly) == true then
                     if (full and HOUND.Mist.pointInPolygon(oPoint, poly) == true) or not full then
                        return true

                    end

                end
            end
        end
        return false
    end
    function HOUND.Mist.shape.polyInPoly(p1, p2, full)
        local count = 0
        for i = 1, #p1 do

            if HOUND.Mist.pointInPolygon(p1[i], p2) then
                count = count + 1
            end
            if (not full) and count > 0 then
                return true
            end
        end
        if count == #p1 then
            return true
        end

        return false
    end

    function HOUND.Mist.shape.polyInCircle(poly, circle, full)
            local count = 0
            for i = 1, #poly do
                if HOUND.Mist.utils.get2DDist(circle.point, poly[i]) <= circle.radius then
                    if full then
                        count = count + 1
                    else
                       return true
                    end
                end
            end
            if count == #poly then
                return true
            end

        return false
    end

    function HOUND.Mist.shape.getPointOnSegment(point, seg, isSeg)
        local p = HOUND.Mist.utils.makeVec2(point)
        local s1 = HOUND.Mist.utils.makeVec2(seg[1])
        local s2 = HOUND.Mist.utils.makeVec2(seg[2])
        local cx, cy = p.x - s1.x, p.y - s1.y
        local dx, dy = s2.x - s1.x, s2.y - s1.y
        local d = (dx*dx + dy*dy)

        if d == 0 then
           return {x = s1.x, y = s1.y}
        end
        local u = (cx*dx + cy*dy)/d
        if isSeg then
           if u < 0 then
                u = 0
            elseif u > 1 then
                u = 1
            end
        end
        return {x = s1.x + u*dx, y = s1.y + u*dy}
    end

    function HOUND.Mist.shape.segmentIntersect(seg1, seg2)
        local segA = {HOUND.Mist.utils.makeVec2(seg1[1]), HOUND.Mist.utils.makeVec2(seg1[2])}
        local segB = {HOUND.Mist.utils.makeVec2(seg2[1]), HOUND.Mist.utils.makeVec2(seg2[2])}

        local dx1, dy1 = segA[2].x - segA[1].x, segA[2].y - segA[1].y
        local dx2, dy2 = segB[2].x - segB[1].x, segB[2].y - segB[1].y
        local dx3, dy3 = segA[1].x - segB[1].x, segA[1].y - segB[1].y

        local d = dx1*dy2 - dy1*dx2

        if d == 0 then
           return false
        end
        local t1 = (dx2*dy3 - dy2*dx3)/d
        if t1 < 0 or t1 > 1 then
          return false
        end
        local t2 = (dx1*dy3 - dy1*dx3)/d
        if t2 < 0 or t2 > 1 then
          return false
        end
          return true, {x = segA[1].x + t1*dx1, y = segA[1].y + t1*dy1}
    end
    function HOUND.Mist.pointInPolygon(point, poly, maxalt) --raycasting point in polygon. Code from http://softsurfer.com/Archive/algorithm_0103/algorithm_0103.htm

        point = HOUND.Mist.utils.makeVec3(point)
        local px = point.x
        local pz = point.z
        local cn = 0
        local newpoly = HOUND.Mist.utils.deepCopy(poly)

        if not maxalt or (point.y <= maxalt) then
            local polysize = #newpoly
            newpoly[#newpoly + 1] = newpoly[1]

            newpoly[1] = HOUND.Mist.utils.makeVec3(newpoly[1])

            for k = 1, polysize do
                newpoly[k+1] = HOUND.Mist.utils.makeVec3(newpoly[k+1])
                if ((newpoly[k].z <= pz) and (newpoly[k+1].z > pz)) or ((newpoly[k].z > pz) and (newpoly[k+1].z <= pz)) then
                    local vt = (pz - newpoly[k].z) / (newpoly[k+1].z - newpoly[k].z)
                    if (px < newpoly[k].x + vt*(newpoly[k+1].x - newpoly[k].x)) then
                        cn = cn + 1
                    end
                end
            end

            return cn%2 == 1
        else
            return false
        end
    end

	function HOUND.Mist.projectPoint(point, dist, theta)
		local newPoint = {}
		if point.z then
		   newPoint.z = HOUND.Mist.utils.round(math.sin(theta) * dist + point.z, 3)
		   newPoint.y = HOUND.Mist.utils.deepCopy(point.y)
		else
		   newPoint.y = HOUND.Mist.utils.round(math.sin(theta) * dist + point.y, 3)
		end
		newPoint.x = HOUND.Mist.utils.round(math.cos(theta) * dist + point.x, 3)

		return newPoint
	end
end

do
	HOUND.Mist.time = {}
	function HOUND.Mist.time.getDHMS(timeInSec)
		if timeInSec and type(timeInSec) == 'number' then
			local tbl = {d = 0, h = 0, m = 0, s = 0}
			if timeInSec > 86400 then
				while timeInSec > 86400 do
					tbl.d = tbl.d + 1
					timeInSec = timeInSec - 86400
				end
			end
			if timeInSec > 3600 then
				while timeInSec > 3600 do
					tbl.h = tbl.h + 1
					timeInSec = timeInSec - 3600
				end
			end
			if timeInSec > 60 then
				while timeInSec > 60 do
					tbl.m = tbl.m + 1
					timeInSec = timeInSec - 60
				end
			end
			tbl.s = timeInSec
			return tbl
		else
			log:error("Didn't recieve number")
			return
		end
	end
end
end

do
    HOUND.Matrix = {}
    HOUND.Matrix.__index = HOUND.Matrix

    function HOUND.Matrix:new( rows, columns, value )
        if type( rows ) == "table" then
            if type(rows[1]) ~= "table" then -- expect a vector
                return setmetatable( {{rows[1]},{rows[2]},{rows[3]}},HOUND.Matrix )
            end
            return setmetatable( rows,HOUND.Matrix )
        end
        local mtx = {}
        local value = value or 0
        if columns == "I" then
            for i = 1,rows do
                mtx[i] = {}
                for j = 1,rows do
                    if i == j then
                        mtx[i][j] = 1
                    else
                        mtx[i][j] = 0
                    end
                end
            end
        else
            for i = 1,rows do
                mtx[i] = {}
                for j = 1,columns do
                    mtx[i][j] = value
                end
            end
        end
        return setmetatable( mtx,HOUND.Matrix )
    end

    setmetatable( HOUND.Matrix, { __call = function( ... ) return HOUND.Matrix.new( ... ) end } )

    function HOUND.Matrix.add( m1, m2 )
        local mtx = {}
        for i = 1,#m1 do
            local m3i = {}
            mtx[i] = m3i
            for j = 1,#m1[1] do
                m3i[j] = m1[i][j] + m2[i][j]
            end
        end
        return setmetatable( mtx, HOUND.Matrix )
    end

    function HOUND.Matrix.sub( m1, m2 )
        local mtx = {}
        for i = 1,#m1 do
            local m3i = {}
            mtx[i] = m3i
            for j = 1,#m1[1] do
                m3i[j] = m1[i][j] - m2[i][j]
            end
        end
        return setmetatable( mtx, HOUND.Matrix )
    end

    function HOUND.Matrix.mul( m1, m2 )
        local mtx = {}
        for i = 1,#m1 do
            mtx[i] = {}
            for j = 1,#m2[1] do
                local num = m1[i][1] * m2[1][j]
                for n = 2,#m1[1] do
                    num = num + m1[i][n] * m2[n][j]
                end
                mtx[i][j] = num
            end
        end
        return setmetatable( mtx, HOUND.Matrix )
    end

    function HOUND.Matrix.div( m1, m2 )
        local rank; m2,rank = HOUND.Matrix.invert( m2 )
        if not m2 then return m2, rank end -- singular
        return HOUND.Matrix.mul( m1, m2 )
    end

    function HOUND.Matrix.mulnum( m1, num )
        local mtx = {}
        for i = 1,#m1 do
            mtx[i] = {}
            for j = 1,#m1[1] do
                mtx[i][j] = m1[i][j] * num
            end
        end
        return setmetatable( mtx, HOUND.Matrix )
    end

    function HOUND.Matrix.divnum( m1, num )
        local mtx = {}
        for i = 1,#m1 do
            local mtxi = {}
            mtx[i] = mtxi
            for j = 1,#m1[1] do
                mtxi[j] = m1[i][j] / num
            end
        end
        return setmetatable( mtx, HOUND.Matrix )
    end

    function HOUND.Matrix.pow( m1, num )
        assert(num == math.floor(num), "exponent not an integer")
        if num == 0 then
            return HOUND.Matrix:new( #m1,"I" )
        end
        if num < 0 then
            local rank; m1,rank = HOUND.Matrix.invert( m1 )
          if not m1 then return m1, rank end -- singular
            num = -num
        end
        local mtx = HOUND.Matrix.copy( m1 )
        for i = 2,num	do
            mtx = HOUND.Matrix.mul( mtx,m1 )
        end
        return mtx
    end

    local function number_norm2(x)
      return x * x
    end

    function HOUND.Matrix.det( m1 )
        assert(#m1 == #m1[1], "matrix not square")

        local size = #m1

        if size == 1 then
            return m1[1][1]
        end

        if size == 2 then
            return m1[1][1]*m1[2][2] - m1[2][1]*m1[1][2]
        end

        if size == 3 then
            return ( m1[1][1]*m1[2][2]*m1[3][3] + m1[1][2]*m1[2][3]*m1[3][1] + m1[1][3]*m1[2][1]*m1[3][2]
                - m1[1][3]*m1[2][2]*m1[3][1] - m1[1][1]*m1[2][3]*m1[3][2] - m1[1][2]*m1[2][1]*m1[3][3] )
        end

        local e = m1[1][1]
        local zero  = type(e) == "table" and e.zero or 0
        local norm2 = type(e) == "table" and e.norm2 or number_norm2

        local mtx = HOUND.Matrix.copy( m1 )
        local det = 1
        for j = 1,#mtx[1] do
            local rows = #mtx
            local subdet,xrow
            for i = 1,rows do
                local e = mtx[i][j]
                if not subdet then
                    if e ~= zero then
                        subdet,xrow = e,i
                    end
                elseif e ~= zero and math.abs(norm2(e)-1) < math.abs(norm2(subdet)-1) then
                    subdet,xrow = e,i
                end
            end
            if subdet then
                if xrow ~= rows then
                    mtx[rows],mtx[xrow] = mtx[xrow],mtx[rows]
                    det = -det
                end
                for i = 1,rows-1 do
                    if mtx[i][j] ~= zero then
                        local factor = mtx[i][j]/subdet
                        for n = j+1,#mtx[1] do
                            mtx[i][n] = mtx[i][n] - factor * mtx[rows][n]
                        end
                    end
                end
                if math.fmod( rows,2 ) == 0 then
                    det = -det
                end
                det = det * subdet
                table.remove( mtx )
            else
                return det * 0
            end
        end
        return det
    end

    local pivotOk = function( mtx,i,j,norm2 )
        local iMin
        local normMin = math.huge
        for _i = i,#mtx do
            local e = mtx[_i][j]
            local norm = math.abs(norm2(e))
            if norm > 0 and norm < normMin then
                iMin = _i
                normMin = norm
                end
            end
        if iMin then
            if iMin ~= i then
                mtx[i],mtx[iMin] = mtx[iMin],mtx[i]
            end
            return true
            end
        return false
    end

    local function copy(x)
        return type(x) == "table" and x.copy(x) or x
    end

    function HOUND.Matrix.dogauss( mtx )
        local e = mtx[1][1]
        local zero = type(e) == "table" and e.zero or 0
        local one  = type(e) == "table" and e.one  or 1
        local norm2 = type(e) == "table" and e.norm2 or number_norm2

        local rows,columns = #mtx,#mtx[1]
        for j = 1,rows do
            if pivotOk( mtx,j,j,norm2 ) then
                for i = j+1,rows do
                    if mtx[i][j] ~= zero then
                        local factor = mtx[i][j]/mtx[j][j]
                        mtx[i][j] = copy(zero)
                        for _j = j+1,columns do
                            mtx[i][_j] = mtx[i][_j] - factor * mtx[j][_j]
                        end
                    end
                end
            else
                return false,j-1
            end
        end
        for j = rows,1,-1 do
            local div = mtx[j][j]
            for _j = j+1,columns do
                mtx[j][_j] = mtx[j][_j] / div
            end
            for i = j-1,1,-1 do
                if mtx[i][j] ~= zero then
                    local factor = mtx[i][j]
                    for _j = j+1,columns do
                        mtx[i][_j] = mtx[i][_j] - factor * mtx[j][_j]
                    end
                    mtx[i][j] = copy(zero)
                end
            end
            mtx[j][j] = copy(one)
        end
        return true
    end

    function HOUND.Matrix.invert( m1 )
        assert(#m1 == #m1[1], "matrix not square")
        local mtx = HOUND.Matrix.copy( m1 )
        local ident = setmetatable( {},HOUND.Matrix )
        local e = m1[1][1]
        local zero = type(e) == "table" and e.zero or 0
        local one  = type(e) == "table" and e.one  or 1
        for i = 1,#m1 do
            local identi = {}
            ident[i] = identi
            for j = 1,#m1 do
                identi[j] = copy((i == j) and one or zero)
            end
        end
        mtx = HOUND.Matrix.concath( mtx,ident )
        local done,rank = HOUND.Matrix.dogauss( mtx )
        if done then
            return HOUND.Matrix.subm( mtx, 1,(#mtx[1]/2)+1,#mtx,#mtx[1] )
        else
            return nil,rank
        end
    end

    local function get_abs_avg( m1, m2 )
        local dist = 0
        local e = m1[1][1]
        local abs = type(e) == "table" and e.abs or math.abs
        for i=1,#m1 do
            for j=1,#m1[1] do
                dist = dist + abs(m1[i][j]-m2[i][j])
            end
        end
        return dist/(#m1*2)
    end
    function HOUND.Matrix.sqrt( m1, iters )
        assert(#m1 == #m1[1], "matrix not square")
        local iters = iters or math.huge
        local y = HOUND.Matrix.copy( m1 )
        local z = HOUND.Matrix(#y, 'I')
        local dist = math.huge
        for n=1,iters do
            local lasty,lastz = y,z
            y, z = HOUND.Matrix.divnum((HOUND.Matrix.add(y,HOUND.Matrix.invert(z))),2),
                    HOUND.Matrix.divnum((HOUND.Matrix.add(z,HOUND.Matrix.invert(y))),2)
            local dist1 = get_abs_avg(y,lasty)
            if iters == math.huge then
                if dist1 >= dist then
                    return lasty,lastz,get_abs_avg(HOUND.Matrix.mul(lasty,lasty),m1)
                end
            end
            dist = dist1
        end
        return y,z,get_abs_avg(HOUND.Matrix.mul(y,y),m1)
    end

    function HOUND.Matrix.root( m1, root, iters )
        assert(#m1 == #m1[1], "matrix not square")
        local iters = iters or math.huge
        local mx = HOUND.Matrix.copy( m1 )
        local my = HOUND.Matrix.mul(mx:invert(),mx:pow(root-1))
        local dist = math.huge
        for n=1,iters do
            local lastx,lasty = mx,my
            mx,my = mx:mulnum(root-1):add(my:invert()):divnum(root),
                my:mulnum(root-1):add(mx:invert()):divnum(root)
                    :mul(my:invert():pow(root-2)):mul(my:mulnum(root-1)
                    :add(mx:invert())):divnum(root)
            local dist1 = get_abs_avg(mx,lastx)
            if iters == math.huge then
                if dist1 >= dist then
                    return lastx,lasty,get_abs_avg(HOUND.Matrix.pow(lastx,root),m1)
                end
            end
            dist = dist1
        end
        return mx,my,get_abs_avg(HOUND.Matrix.pow(mx,root),m1)
    end

    function HOUND.Matrix.normf(mtx)
        local mtype = HOUND.Matrix.type(mtx)
        local result = 0
        for i = 1,#mtx do
        for j = 1,#mtx[1] do
            local e = mtx[i][j]
            if mtype ~= "number" then e = e:abs() end
            result = result + e^2
        end
        end
        local sqrt = (type(result) == "number") and math.sqrt or result.sqrt
        return sqrt(result)
    end

    function HOUND.Matrix.normmax(mtx)
        local abs = (HOUND.Matrix.type(mtx) == "number") and math.abs or mtx[1][1].abs
        local result = 0
        for i = 1,#mtx do
        for j = 1,#mtx[1] do
            local e = abs(mtx[i][j])
            if e > result then result = e end
        end
        end
        return result
    end

    local numround = function( num,mult )
        return math.floor( num * mult + 0.5 ) / mult
    end
    local tround = function( t,mult )
        for i,v in ipairs(t) do
            t[i] = math.floor( v * mult + 0.5 ) / mult
        end
        return t
    end
    function HOUND.Matrix.round( mtx, idp )
        local mult = 10^( idp or 0 )
        local fround = HOUND.Matrix.type( mtx ) == "number" and numround or tround
        for i = 1,#mtx do
            for j = 1,#mtx[1] do
                mtx[i][j] = fround(mtx[i][j],mult)
            end
        end
        return mtx
    end

    local numfill = function( _,start,stop,idp )
        return l_math.random( start,stop ) / idp
    end
    local tfill = function( t,start,stop,idp )
        for i in ipairs(t) do
            t[i] = l_math.random( start,stop ) / idp
        end
        return t
    end
    function HOUND.Matrix.random( mtx,start,stop,idp )
        local start,stop,idp = start or -10,stop or 10,idp or 1
        local ffill = HOUND.Matrix.type( mtx ) == "number" and numfill or tfill
        for i = 1,#mtx do
            for j = 1,#mtx[1] do
                mtx[i][j] = ffill( mtx[i][j], start, stop, idp )
            end
        end
        return mtx
    end

    function HOUND.Matrix.type( mtx )
        local e = mtx[1][1]
        if type(e) == "table" then
            if e.type then
                return e:type()
            end
            return "tensor"
        end
        return "number"
    end

    local num_copy = function( num )
        return num
    end
    local t_copy = function( t )
        local newt = setmetatable( {}, getmetatable( t ) )
        for i,v in ipairs( t ) do
            newt[i] = v
        end
        return newt
    end

    function HOUND.Matrix.copy( m1 )
        local docopy = HOUND.Matrix.type( m1 ) == "number" and num_copy or t_copy
        local mtx = {}
        for i = 1,#m1[1] do
            mtx[i] = {}
            for j = 1,#m1 do
                mtx[i][j] = docopy( m1[i][j] )
            end
        end
        return setmetatable( mtx, HOUND.Matrix )
    end

    function HOUND.Matrix.transpose( m1 )
        local docopy = HOUND.Matrix.type( m1 ) == "number" and num_copy or t_copy
        local mtx = {}
        for i = 1,#m1[1] do
            mtx[i] = {}
            for j = 1,#m1 do
                mtx[i][j] = docopy( m1[j][i] )
            end
        end
        return setmetatable( mtx, HOUND.Matrix )
    end

    function HOUND.Matrix.subm( m1,i1,j1,i2,j2 )
        local docopy = HOUND.Matrix.type( m1 ) == "number" and num_copy or t_copy
        local mtx = {}
        for i = i1,i2 do
            local _i = i-i1+1
            mtx[_i] = {}
            for j = j1,j2 do
                local _j = j-j1+1
                mtx[_i][_j] = docopy( m1[i][j] )
            end
        end
        return setmetatable( mtx, HOUND.Matrix )
    end

    function HOUND.Matrix.concath( m1,m2 )
        assert(#m1 == #m2, "matrix size mismatch")
        local docopy = HOUND.Matrix.type( m1 ) == "number" and num_copy or t_copy
        local mtx = {}
        local offset = #m1[1]
        for i = 1,#m1 do
            mtx[i] = {}
            for j = 1,offset do
                mtx[i][j] = docopy( m1[i][j] )
            end
            for j = 1,#m2[1] do
                mtx[i][j+offset] = docopy( m2[i][j] )
            end
        end
        return setmetatable( mtx, HOUND.Matrix )
    end

    function HOUND.Matrix.concatv( m1,m2 )
        assert(#m1[1] == #m2[1], "matrix size mismatch")
        local docopy = HOUND.Matrix.type( m1 ) == "number" and num_copy or t_copy
        local mtx = {}
        for i = 1,#m1 do
            mtx[i] = {}
            for j = 1,#m1[1] do
                mtx[i][j] = docopy( m1[i][j] )
            end
        end
        local offset = #mtx
        for i = 1,#m2 do
            local _i = i + offset
            mtx[_i] = {}
            for j = 1,#m2[1] do
                mtx[_i][j] = docopy( m2[i][j] )
            end
        end
        return setmetatable( mtx, HOUND.Matrix )
    end

    function HOUND.Matrix.rotl( m1 )
        local mtx = HOUND.Matrix:new( #m1[1],#m1 )
        local docopy = HOUND.Matrix.type( m1 ) == "number" and num_copy or t_copy
        for i = 1,#m1 do
            for j = 1,#m1[1] do
                mtx[#m1[1]-j+1][i] = docopy( m1[i][j] )
            end
        end
        return mtx
    end

    function HOUND.Matrix.rotr( m1 )
        local mtx = HOUND.Matrix:new( #m1[1],#m1 )
        local docopy = HOUND.Matrix.type( m1 ) == "number" and num_copy or t_copy
        for i = 1,#m1 do
            for j = 1,#m1[1] do
                mtx[j][#m1-i+1] = docopy( m1[i][j] )
            end
        end
        return mtx
    end

    local function tensor_tostring( t,fstr )
        if not fstr then return "["..table.concat(t,",").."]" end
        local tval = {}
        for i,v in ipairs( t ) do
            tval[i] = string.format( fstr,v )
        end
        return "["..table.concat(tval,",").."]"
    end
    local function number_tostring( e,fstr )
        return fstr and string.format( fstr,e ) or e
    end

    function HOUND.Matrix.tostring( mtx, formatstr )
        local ts = {}
        local mtype = HOUND.Matrix.type( mtx )
        local e = mtx[1][1]
        local tostring = mtype == "tensor" and tensor_tostring or
              type(e) == "table" and e.tostring or number_tostring
        for i = 1,#mtx do
            local tstr = {}
            for j = 1,#mtx[1] do
                tstr[j] = tostring(mtx[i][j],formatstr)
            end
            ts[i] = table.concat(tstr, "\t")
        end
        return table.concat(ts, "\n")
    end

    function HOUND.Matrix.latex( mtx, align )
        local align = align or "c"
        local str = "$\\left( \\begin{array}{"..string.rep( align, #mtx[1] ).."}\n"
        local getstr = HOUND.Matrix.type( mtx ) == "tensor" and tensor_tostring or number_tostring
        for i = 1,#mtx do
            str = str.."\t"..getstr(mtx[i][1])
            for j = 2,#mtx[1] do
                str = str.." & "..getstr(mtx[i][j])
            end
            if i == #mtx then
                str = str.."\n"
            else
                str = str.." \\\\\n"
            end
        end
        return str.."\\end{array} \\right)$"
    end

    function HOUND.Matrix.rows( mtx )
        return #mtx
    end

    function HOUND.Matrix.columns( mtx )
        return #mtx[1]
    end

    function HOUND.Matrix.size( mtx )
        if HOUND.Matrix.type( mtx ) == "tensor" then
            return #mtx,#mtx[1],#mtx[1][1]
        end
        return #mtx,#mtx[1]
    end

    function HOUND.Matrix.getelement( mtx,i,j )
        if mtx[i] and mtx[i][j] then
            return mtx[i][j]
        end
    end

    function HOUND.Matrix.setelement( mtx,i,j,value )
        if HOUND.Matrix.getelement( mtx,i,j ) then
            mtx[i][j] = value
            return 1
        end
    end

    function HOUND.Matrix.ipairs( mtx )
        local i,j,rows,columns = 1,0,#mtx,#mtx[1]
        local function iter()
            j = j + 1
            if j > columns then -- return first element from next row
                i,j = i + 1,1
            end
            if i <= rows then
                return i,j
            end
        end
        return iter
    end

    function HOUND.Matrix.scalar( m1, m2 )
        return m1[1][1]*m2[1][1] + m1[2][1]*m2[2][1] +  m1[3][1]*m2[3][1]
    end

    function HOUND.Matrix.cross( m1, m2 )
        local mtx = {}
        mtx[1] = { m1[2][1]*m2[3][1] - m1[3][1]*m2[2][1] }
        mtx[2] = { m1[3][1]*m2[1][1] - m1[1][1]*m2[3][1] }
        mtx[3] = { m1[1][1]*m2[2][1] - m1[2][1]*m2[1][1] }
        return setmetatable( mtx, HOUND.Matrix )
    end

    function HOUND.Matrix.len( m1 )
        return math.sqrt( m1[1][1]^2 + m1[2][1]^2 + m1[3][1]^2 )
    end

    function HOUND.Matrix.replace( m1, func, ... )
        local mtx = {}
        for i = 1,#m1 do
            local m1i = m1[i]
            local mtxi = {}
            for j = 1,#m1i do
                mtxi[j] = func( m1i[j], ... )
            end
            mtx[i] = mtxi
        end
        return setmetatable( mtx, HOUND.Matrix )
    end

    function HOUND.Matrix.elementstostrings( mtx )
        local e = mtx[1][1]
        local tostring = type(e) == "table" and e.tostring or tostring
        return HOUND.Matrix.replace(mtx, tostring)
    end

    function HOUND.Matrix.solve( m1 )
        assert( HOUND.Matrix.type( m1 ) == "symbol", "matrix not of type 'symbol'" )
        local mtx = {}
        for i = 1,#m1 do
            mtx[i] = {}
            for j = 1,#m1[1] do
                mtx[i][j] = tonumber( loadstring( "return "..m1[i][j][1] )() )
            end
        end
        return setmetatable( mtx, HOUND.Matrix )
    end

    HOUND.Matrix.__add = function( ... )
        return HOUND.Matrix.add( ... )
    end

    HOUND.Matrix.__sub = function( ... )
        return HOUND.Matrix.sub( ... )
    end

    HOUND.Matrix.__mul = function( m1,m2 )
        if getmetatable( m1 ) ~= HOUND.Matrix then
            return HOUND.Matrix.mulnum( m2,m1 )
        elseif getmetatable( m2 ) ~= HOUND.Matrix then
            return HOUND.Matrix.mulnum( m1,m2 )
        end
        return HOUND.Matrix.mul( m1,m2 )
    end

    HOUND.Matrix.__div = function( m1,m2 )
        if getmetatable( m1 ) ~= HOUND.Matrix then
            return HOUND.Matrix.mulnum( HOUND.Matrix.invert(m2),m1 )
        elseif getmetatable( m2 ) ~= HOUND.Matrix then
            return HOUND.Matrix.divnum( m1,m2 )
        end
        return HOUND.Matrix.div( m1,m2 )
    end

    HOUND.Matrix.__unm = function( mtx )
        return HOUND.Matrix.mulnum( mtx,-1 )
    end

        local option = {
            ["*"] = function( m1 ) return HOUND.Matrix.conjugate( m1 ) end,
            ["T"] = function( m1 ) return HOUND.Matrix.transpose( m1 ) end,
        }
    HOUND.Matrix.__pow = function( m1, opt )
        return option[opt] and option[opt]( m1 ) or HOUND.Matrix.pow( m1,opt )
    end

    HOUND.Matrix.__eq = function( m1, m2 )
        if HOUND.Matrix.type( m1 ) ~= HOUND.Matrix.type( m2 ) then
            return false
        end
        if #m1 ~= #m2 or #m1[1] ~= #m2[1] then
            return false
        end
        for i = 1,#m1 do
            for j = 1,#m1[1] do
                if m1[i][j] ~= m2[i][j] then
                    return false
                end
            end
        end
        return true
    end

end--- Hound databases

do
    HOUND.DB = {}

    HOUND.DB.PHONETICS =  {
        ['A'] = "Alpha",
        ['B'] = "Bravo",
        ['C'] = "Charlie",
        ['D'] = "Delta",
        ['E'] = "Echo",
        ['F'] = "Foxtrot",
        ['G'] = "Golf",
        ['H'] = "Hotel",
        ['I'] = "India",
        ['J'] = "Juliette",
        ['K'] = "Kilo",
        ['L'] = "Lima",
        ['M'] = "Mike",
        ['N'] = "November",
        ['O'] = "Oscar",
        ['P'] = "Papa",
        ['Q'] = "Quebec",
        ['R'] = "Romeo",
        ['S'] = "Sierra",
        ['T'] = "Tango",
        ['U'] = "Uniform",
        ['V'] = "Victor",
        ['W'] = "Whiskey",
        ['X'] = "X ray",
        ['Y'] = "Yankee",
        ['Z'] = "Zulu",
        ['1'] = "One",
        ['2'] = "Two",
        ['3'] = "Three",
        ['4'] = "Four",
        ['5'] = "Five",
        ['6'] = "Six",
        ['7'] = "Seven",
        ['8'] = "Eight",
        ['9'] = "Niner",
        ['0'] = "Zero",
        [' '] = ",",
        ['.'] = "Decimal"
    }

    HOUND.DB.useDMM =  {
        ['F-16C_blk50'] = true,
        ['F-16C_50'] = true,
        ['M-2000C'] = true,
        ['A-10C'] = true,
        ['A-10C_2'] = true,
        ['AH-64D_BLK_II'] = true,
        ['F-15ESE'] = true,
        ['OH58D'] = true,
        ['OH-58D'] = true
    }

    HOUND.DB.useMGRS = {
        ['A-10C'] = true,
        ['A-10C_2'] = true,
        ['AH-64D_BLK_II'] = true,
        ['OH58D'] = true,
        ['OH-58D'] = true
    }

    HOUND.DB.Bands = {
        ["A"] = {1.199170,8.793912},
        ["B"] = {0.599585,0.599585},
        ["C"] = {0.299792,0.299792},
        ["D"] = {0.149896,0.149896},
        ["E"] = {0.099931,0.049965},
        ["F"] = {0.074948,0.024983},
        ["G"] = {0.049965,0.024983},
        ["H"] = {0.037474,0.012491},
        ["I"] = {0.029979,0.007495},
        ["J"] = {0.014990,0.014990},
        ["K"] = {0.007495,0.007495},
        ["L"] = {0.004997,0.002498},
        ["M"] = {0.002998,0.001999},
    }

    HOUND.DB.RadarType = {
        ['NONE'] = 0x00,
        ['EWR'] = 0x01,
        ['RANGEFINDER'] = 0x02,
        ['ANTISHIP'] = 0x04,
        ['SEARCH'] = 0x08,
        ['TRACK'] = 0x10,
        ['NAVAL'] = 0x20
    }

    HOUND.DB.CALLSIGNS = {
        NATO = {
            "ABLOW", "ACTON", "AGRAM", "AMINO", "AWOKE", "BARB", "BART", "BAZOO",
            "BOGUE", "BOOT", "BRAY", "CAMAY", "CAPON", "CASEY", "CHIME", "CHISUM",
            "COBRA", "COSMO", "CRISP", "DAGDA", "DALLY", "DEVON", "DIVE", "DOZER",
            "DUPLE", "EXOR", "EXUDE", "EXULT", "FLOSS", "FLOUT", "FLUKY", "FURR",
            "GENUS", "GOBO", "GOLLY", "GOOFY", "GROUP", "HAKE", "HARMO",
            "HERMA", "HEXAD", "HOLE", "HURDS", "HYMN", "IOTA", "JOSS", "KELT", "LARVA",
            "LUMPY", "MAFIA", "MINE", "MORTY", "MURKY", "NEVIN", "NEWLY", "NORTH",
            "OLIVE", "ORKIN", "PARRY", "PATIO", "PATSY", "PATTY", "PERMA", "PITTS",
            "POKER", "POOK", "PRIME", "PYTHON", "RAGU", "REMUS", "RINGY", "RITZ",
            "RIVET", "ROSE", "RULE", "RUNNY", "SAME", "SAVOY", "SCENT",
            "SCROW", "SEAT", "SLAG", "SLOG", "SNOOP", "SPRY", "STINT", "STOB", "TAKE",
            "TALLY", "TAPE", "TOLL", "TONUS", "TOPCAT", "TORA", "TOTTY", "TOXIC",
            "TRIAL", "TRYST", "VALVO", "VEIN", "VELA", "VETCH", "VINE", "VULCAN",
            "WATT", "WORTH", "ZEPEL", "ZIPPY"
        },
        GENERIC = {
            "VACUUM", "HOOVER", "KIRBY","ROOMBA","DYSON","SHERLOCK","WATSON","GADGET",
            "HORATIO","CAINE","CHRISTIE","BENSON","GIBBS","COLOMBO","HOLT","DIAZ",
            "SCULLY","MULDER","MARVIN","MARS","MORNINGSTAR","STEELE","CASTEL","BECKETT",
            "INDIANA","JONES","LARA","CROFT","VENTURA","SCOOBY","SHAGGY"
        }
    }

    HOUND.DB.HumanUnits = {
        byName = {
            [coalition.side.NEUTRAL] = {},
            [coalition.side.RED] = {},
            [coalition.side.BLUE] = {}
        },
        byGid = {
            [coalition.side.NEUTRAL] = {},
            [coalition.side.RED] = {},
            [coalition.side.BLUE] = {}
        }
    }
end
do
    HOUND.DB.Radars = {
        ['1L13 EWR'] = {
            ['Name'] = "Box Spring",
            ['Assigned'] = { "EWR" },
            ['Role'] = { HOUND.DB.RadarType.EWR },
            ['Band'] = {
                [true] = { 1.362693, 0.302821 },
                [false] = { 1.362693, 0.302821 },
            },
            ['Primary'] = false,
            ['numDistinctFreqs'] = 4
        },
        ['55G6 EWR'] = {
            ['Name'] = "Tall Rack",
            ['Assigned'] = { "EWR" },
            ['Role'] = { HOUND.DB.RadarType.EWR },
            ['Band'] = {
                [true] = { 0.999308, 8.993774 },
                [false] = { 0.999308, 8.993774 }
            },
            ['Primary'] = false,
            ['numDistinctFreqs'] = 4
        },
        ['FPS-117'] = {
            ['Name'] = "Seek Igloo",
            ['Assigned'] = { "EWR" },
            ['Role'] = { HOUND.DB.RadarType.EWR },
            ['Band'] = {
                [true] = { 0.214137, 0.032605 },
                [false] = { 0.214137, 0.032605 },
            },
            ['Primary'] = false,
            ['numDistinctFreqs'] = 0
        },
        ['FPS-117 Dome'] = {
            ['Name'] = "Seek Igloo",
            ['Assigned'] = { "EWR" },
            ['Role'] = { HOUND.DB.RadarType.EWR },
            ['Band'] = {
                [true] = { 0.214137, 0.032605 },
                [false] = { 0.214137, 0.032605 },
            },
            ['Primary'] = false,
            ['numDistinctFreqs'] = 0
        },
        ['p-19 s-125 sr'] = {
            ['Name'] = "Flat Face",
            ['Assigned'] = { "SA-2", "SA-3" },
            ['Role'] = { HOUND.DB.RadarType.SEARCH },
            ['Band'] = {
                [true] = { 0.342620, 0.018576 },
                [false] = { 0.342620, 0.018576 }
            },
            ['Primary'] = false,
            ['numDistinctFreqs'] = 4
        },
        ['SNR_75V'] = {
            ['Name'] = "Fan-song",
            ['Assigned'] = { "SA-2" },
            ['Role'] = { HOUND.DB.RadarType.TRACK },
            ['Band'] = {
                [true] = { 0.058898, 0.002159 },
                [false] = { 0.058898, 0.000940 }
            },
            ['Primary'] = true,
            ['numDistinctFreqs'] = 2
        },
        ['RD_75'] = {
            ['Name'] = "Amazonka",
            ['Assigned'] = { "SA-2" },
            ['Role'] = { HOUND.DB.RadarType.RANGEFINDER },
            ['Band'] = {
                [true] = HOUND.DB.Bands.G,
                [false] = HOUND.DB.Bands.G
            },
            ['Primary'] = false,
            ['numDistinctFreqs'] = 2
        },
        ['snr s-125 tr'] = {
            ['Name'] = "Low Blow",
            ['Assigned'] = { "SA-3" },
            ['Role'] = { HOUND.DB.RadarType.TRACK },
            ['Band'] = {
                [true] = { 0.031893, 0.001417 },
                [false] = { 0.031893, 0.001417 }
            },
            ['Primary'] = true,
            ['numDistinctFreqs'] = 2
        },
        ['Kub 1S91 str'] = {
            ['Name'] = "Straight Flush",
            ['Assigned'] = { "SA-6" },
            ['Role'] = { HOUND.DB.RadarType.SEARCH, HOUND.DB.RadarType.TRACK },
            ['Band'] = {
                [true] = HOUND.DB.Bands.I,
                [false] = { 0.033310, 0.004164 }
            },
            ['Primary'] = true,
            ['numDistinctFreqs'] = 4
        },
        ['Osa 9A33 ln'] = {
            ['Name'] = "Osa",
            ['Assigned'] = { "SA-8" },
            ['Role'] = { HOUND.DB.RadarType.SEARCH, HOUND.DB.RadarType.TRACK },
            ['Band'] = {
                [true] = { 0.020256, 0.000856 },
                [false] = HOUND.DB.Bands.H
            },
            ['Primary'] = true,
            ['numDistinctFreqs'] = 4
        },
        ['S-300PS 40B6MD sr'] = {
            ['Name'] = "Clam Shell",
            ['Assigned'] = { "SA-10" },
            ['Role'] = { HOUND.DB.RadarType.SEARCH },
            ['Band'] = {
                [true] = { 0.090846, 0.012531 },
                [false] = { 0.090846, 0.012531 }
            },
            ['Primary'] = false,
            ['numDistinctFreqs'] = 0
        },
        ['S-300PS 64H6E sr'] = {
            ['Name'] = "Big Bird",
            ['Assigned'] = { "SA-10" },
            ['Role'] = { HOUND.DB.RadarType.SEARCH },
            ['Band'] = {
                [true] = { 0.090846, 0.012531 },
                [false] = { 0.090846, 0.012531 }
            },
            ['Primary'] = false,
            ['numDistinctFreqs'] = 0
        },
        ['RLS_19J6'] = {
            ['Name'] = "Tin Shield",
            ['Assigned'] = { "SA-5" },
            ['Role'] = { HOUND.DB.RadarType.SEARCH },
            ['Band'] = {
                [true] = { 0.093685, 0.011505 },
                [false] = { 0.093685, 0.011505 }
            },
            ['Primary'] = false,
            ['numDistinctFreqs'] = 0
        },
        ['P14_SR'] = {
            ['Name'] = "Tall king",
            ['Assigned'] = { "SA-5" },
            ['Role'] = { HOUND.DB.RadarType.SEARCH },
            ['Band'] = {
                    [true] = {1.620500,0.253203},
                    [false] = {1.620500,0.253203}
            },
            ['Primary'] = false,
            ['numDistinctFreqs'] = 4
        },
        ['S-300PS 40B6MD sr_19J6'] = {
            ['Name'] = "Tin Shield",
            ['Assigned'] = { "SA-10" },
            ['Role'] = { HOUND.DB.RadarType.SEARCH },
            ['Band'] = {
                [true] = { 0.093685, 0.011505 },
                [false] = { 0.093685, 0.011505 }
            },
            ['Primary'] = false,
            ['numDistinctFreqs'] = 0
        },
        ['S-300PS 40B6M tr'] = {
            ['Name'] = "Tomb Stone",
            ['Assigned'] = { "SA-10" },
            ['Role'] = { HOUND.DB.RadarType.TRACK },
            ['Band'] = {
                [true] = { 0.014990, 0.022484 },
                [false] = { 0.014990, 0.022484 }
            },
            ['Primary'] = true,
            ['numDistinctFreqs'] = 0
        },
        ['S-300PS 5H63C 30H6_tr'] = {
            ['Name'] = "Flap Lid",
            ['Assigned'] = { "SA-10" },
            ['Role'] = { HOUND.DB.RadarType.TRACK },
            ['Band'] = {
                [true] = { 0.014990, 0.022484 },
                [false] = { 0.014990, 0.022484 }
            },
            ['Primary'] = true,
            ['numDistinctFreqs'] = 0
        },
        ['SA-11 Buk SR 9S18M1'] = {
            ['Name'] = "Snow Drift",
            ['Assigned'] = { "SA-11" },
            ['Role'] = { HOUND.DB.RadarType.SEARCH },
            ['Band'] = {
                [true] = { 0.033310, 0.016655 },
                [false] = HOUND.DB.Bands.F
            },
            ['Primary'] = true,
            ['numDistinctFreqs'] = 0
        },
        ['SA-11 Buk LN 9A310M1'] = {
            ['Name'] = "Fire Dome",
            ['Assigned'] = { "SA-11" },
            ['Role'] = { HOUND.DB.RadarType.TRACK },
            ['Band'] = {
                [true] = { 0.033310, 0.016655 },
                [false] = { 0.029979, 0.019986 }
            },
            ['Primary'] = false,
            ['numDistinctFreqs'] = 0
        },
        ['Tor 9A331'] = {
            ['Name'] = "Tor",
            ['Assigned'] = { "SA-15" },
            ['Role'] = { HOUND.DB.RadarType.SEARCH, HOUND.DB.RadarType.TRACK },
            ['Band'] = {
                [true] = { 0.037474, 0.037474 }, -- G+H
                [false] = HOUND.DB.Bands.F
            },
            ['Primary'] = true,
            ['numDistinctFreqs'] = 0
        },
        ['Strela-1 9P31'] = {
            ['Name'] = "SA-9",
            ['Assigned'] = { "Strela" },
            ['Role'] = { HOUND.DB.RadarType.RANGEFINDER },
            ['Band'] = {
                [true] = HOUND.DB.Bands.K,
                [false] = HOUND.DB.Bands.K
            },
            ['Primary'] = false,
            ['numDistinctFreqs'] = 0
        },
        ['Strela-10M3'] = {
            ['Name'] = "SA-13",
            ['Assigned'] = { "Strela" },
            ['Role'] = { HOUND.DB.RadarType.RANGEFINDER },
            ['Band'] = {
                [true] = HOUND.DB.Bands.J,
                [false] = HOUND.DB.Bands.J
            },
            ['Primary'] = false,
            ['numDistinctFreqs'] = 0
        },
        ['Patriot str'] = {
            ['Name'] = "Patriot",
            ['Assigned'] = { "Patriot" },
            ['Role'] = { HOUND.DB.RadarType.SEARCH, HOUND.DB.RadarType.TRACK },
            ['Band'] = {
                [true] = { 0.055008, 0.011910 },
                [false] = { 0.055008, 0.011910 }
            },
            ['Primary'] = true,
            ['numDistinctFreqs'] = 0
        },
        ['Hawk sr'] = {
            ['Name'] = "Hawk SR",
            ['Assigned'] = { "Hawk" },
            ['Role'] = { HOUND.DB.RadarType.SEARCH },
            ['Band'] = {
                [true] = HOUND.DB.Bands.C,
                [false] = HOUND.DB.Bands.C
            },
            ['Primary'] = false,
            ['numDistinctFreqs'] = 6
        },
        ['Hawk tr'] = {
            ['Name'] = "Hawk TR",
            ['Assigned'] = { "Hawk" },
            ['Role'] = { HOUND.DB.RadarType.TRACK },
            ['Band'] = {
                [true] = { 0.024983, 0.012491 },
                [false] = { 0.024983, 0.012491 }
            },
            ['Primary'] = true,
            ['numDistinctFreqs'] = 6
        },
        ['Hawk cwar'] = {
            ['Name'] = "Hawk CWAR",
            ['Assigned'] = { "Hawk" },
            ['Role'] = { HOUND.DB.RadarType.TRACK },
            ['Band'] = {
                [true] = HOUND.DB.Bands.J,
                [false] = HOUND.DB.Bands.J
            },
            ['Primary'] = false,
            ['numDistinctFreqs'] = 6
        },
        ['RPC_5N62V'] = {
            ['Name'] = "Square Pair",
            ['Assigned'] = { "SA-5" },
            ['Role'] = { HOUND.DB.RadarType.TRACK },
            ['Band'] = {
                [true] = { 0.044087, 0.002755 },
                [false] = { 0.044087, 0.002755 }
            },
            ['Primary'] = true,
            ['numDistinctFreqs'] = 4
        },
        ['Roland ADS'] = {
            ['Name'] = "Roland TR",
            ['Assigned'] = { "Roland" },
            ['Role'] = { HOUND.DB.RadarType.TRACK },
            ['Band'] = {
                [true] = { 0.024983, 0.012491 },
                [false] = HOUND.DB.Bands.D
            },
            ['Primary'] = true,
            ['numDistinctFreqs'] = 0
        },
        ['Roland Radar'] = {
            ['Name'] = "Roland SR",
            ['Assigned'] = { "Roland" },
            ['Role'] = { HOUND.DB.RadarType.SEARCH },
            ['Band'] = {
                [true] = HOUND.DB.Bands.D,
                [false] = HOUND.DB.Bands.D
            },
            ['Primary'] = false,
            ['numDistinctFreqs'] = 0
        },
        ['Gepard'] = {
            ['Name'] = "Gepard",
            ['Assigned'] = { "Gepard" },
            ['Role'] = { HOUND.DB.RadarType.SEARCH, HOUND.DB.RadarType.TRACK },
            ['Band'] = {
                [true] = HOUND.DB.Bands.J,
                [false] = HOUND.DB.Bands.E
            },
            ['Primary'] = true,
            ['numDistinctFreqs'] = 0
        },
        ['rapier_fsa_blindfire_radar'] = {
            ['Name'] = "Rapier",
            ['Assigned'] = { "Rapier" },
            ['Role'] = { HOUND.DB.RadarType.SEARCH, HOUND.DB.RadarType.TRACK },
            ['Band'] = {
                [true] = HOUND.DB.Bands.F,
                [false] = HOUND.DB.Bands.F
            },
            ['Primary'] = true,
            ['numDistinctFreqs'] = 0
        },
        ['rapier_fsa_launcher'] = {
            ['Name'] = "Rapier",
            ['Assigned'] = { "Rapier" },
            ['Role'] = { HOUND.DB.RadarType.TRACK },
            ['Band'] = {
                [true] = { 0.074948, 0.224844 },
                [false] = { 0.074948, 0.224844 }
            },
            ['Primary'] = false,
            ['numDistinctFreqs'] = 0
        },
        ['NASAMS_Radar_MPQ64F1'] = {
            ['Name'] = "Sentinel",
            ['Assigned'] = { "NASAMS" },
            ['Role'] = { HOUND.DB.RadarType.SEARCH },
            ['Band'] = {
                [true] = { 0.024983, 0.012491 },
                [false] = { 0.024983, 0.012491 }
            },
            ['Primary'] = true,
            ['numDistinctFreqs'] = 0
        },
        ['HQ-7_STR_SP'] = {
            ['Name'] = "HQ-7",
            ['Assigned'] = { "HQ-7" },
            ['Role'] = { HOUND.DB.RadarType.SEARCH },
            ['Band'] = {
                [true] = { 0.516884, 0.082701 },
                [false] = HOUND.DB.Bands.E
            },
            ['Primary'] = false,
            ['numDistinctFreqs'] = 0
        },
        ['HQ-7_LN_SP'] = {
            ['Name'] = "HQ-7",
            ['Assigned'] = { "HQ-7" },
            ['Role'] = { HOUND.DB.RadarType.TRACK },
            ['Band'] = {
                [true] = HOUND.DB.Bands.J,
                [false] = HOUND.DB.Bands.E
            },
            ['Primary'] = true,
            ['numDistinctFreqs'] = 0
        },
        ['HQ-7_LN_P'] = {
            ['Name'] = "HQ-7",
            ['Assigned'] = { "HQ-7" },
            ['Role'] = { HOUND.DB.RadarType.TRACK },
            ['Band'] = {
                [true] = HOUND.DB.Bands.E,
                [false] = HOUND.DB.Bands.E
            },
            ['Primary'] = true,
            ['numDistinctFreqs'] = 0
        },
        ['2S6 Tunguska'] = {
            ['Name'] = "Tunguska",
            ['Assigned'] = { "Tunguska" },
            ['Role'] = { HOUND.DB.RadarType.SEARCH, HOUND.DB.RadarType.TRACK },
            ['Band'] = {
                [true] = HOUND.DB.Bands.J,
                [false] = HOUND.DB.Bands.E
            },
            ['Primary'] = true,
            ['numDistinctFreqs'] = 0
        },
        ['ZSU-23-4 Shilka'] = {
            ['Name'] = "Shilka",
            ['Assigned'] = { "AAA" },
            ['Role'] = { HOUND.DB.RadarType.TRACK },
            ['Band'] = {
                [true] = { 0.019217, 0.001316 },
                [false] = { 0.019217, 0.001316 }
            },
            ['Primary'] = true,
            ['numDistinctFreqs'] = 0
        },
        ['HEMTT_C-RAM_Phalanx'] = {
            ['Name'] = "Phalanx C-RAM",
            ['Assigned'] = { "AAA" },
            ['Role'] = { HOUND.DB.RadarType.TRACK },
            ['Band'] = {
                [true] = { 0.016655, 0.008328 },
                [false] = { 0.016655, 0.008328 }
            },
            ['Primary'] = true,
            ['numDistinctFreqs'] = 0
        },
        ['Dog Ear radar'] = {
            ['Name'] = "Dog Ear",
            ['Assigned'] = { "AAA" },
            ['Role'] = { HOUND.DB.RadarType.SEARCH },
            ['Band'] = {
                [true] = { 0.049965, 0.049965 },
                [false] = { 0.049965, 0.049965 }
            },
            ['Primary'] = true,
            ['numDistinctFreqs'] = 0
        },
        ['SON_9'] = {
            ['Name'] = "Fire Can",
            ['Assigned'] = { "AAA" },
            ['Role'] = { HOUND.DB.RadarType.TRACK },
            ['Band'] = {
                [true] = { 0.103377, 0.007658 },
                [false] = { 0.103377, 0.007658 }
            },
            ['Primary'] = true,
            ['numDistinctFreqs'] = 0
        },
        ['Silkworm_SR'] = {
            ['Name'] = "Silkworm",
            ['Assigned'] = { "Silkworm" },
            ['Role'] = { HOUND.DB.RadarType.ANTISHIP },
            ['Band'] = {
                [true] = HOUND.DB.Bands.K,
                [false] = HOUND.DB.Bands.K
            },
            ['Primary'] = true,
            ['numDistinctFreqs'] = 0
        },
        ['FuSe-65'] = {
            ['Name'] = "Würzburg",
            ['Assigned'] = { "AAA" },
            ['Role'] = { HOUND.DB.RadarType.TRACK },
            ['Band'] = {
                [true] = { 0.535344, 0.000000 },
                [false] = { 0.535344, 0.000000 }
            },
            ['Primary'] = false,
            ['numDistinctFreqs'] = 0
        },
        ['FuMG-401'] = {
            ['Name'] = "EWR",
            ['Assigned'] = { "EWR" },
            ['Role'] = { HOUND.DB.RadarType.EWR },
            ['Band'] = {
                [true] = { 2.306096, 0.192175 },
                [false] = { 2.306096, 0.192175 }
            },
            ['Primary'] = false,
            ['numDistinctFreqs'] = 0
        },
        ['Flakscheinwerfer_37'] = {
            ['Name'] = "AAA Searchlight",
            ['Assigned'] = { "AAA" },
            ['Role'] = { HOUND.DB.RadarType.NONE },
            ['Band'] = {
                [true] = HOUND.DB.Bands.L,
                [false] = HOUND.DB.Bands.L
            },
            ['Primary'] = false,
            ['numDistinctFreqs'] = 0
        },
        ['Type_052B'] = {
            ['Name'] = "Luyang-1 (DD)",
            ['Assigned'] = { "Naval" },
            ['Role'] = { HOUND.DB.RadarType.NAVAL },
            ['Band'] = {
                [true] = { 0.033310, 0.016655 },
                [false] = HOUND.DB.Bands.F
            },
            ['Primary'] = true,
            ['numDistinctFreqs'] = 0
        },
        ['Type_052C'] = {
            ['Name'] = "Luyang-2 (DD)",
            ['Assigned'] = { "Naval" },
            ['Role'] = { HOUND.DB.RadarType.NAVAL },
            ['Band'] = {
                [true] = { 0.516884, 0.082701 },
                [false] = { 0.024983, 0.012491 }
            },
            ['Primary'] = true,
            ['numDistinctFreqs'] = 0
        },
        ['Type_054A'] = {
            ['Name'] = "Jiangkai (FF)",
            ['Assigned'] = { "Naval" },
            ['Role'] = { HOUND.DB.RadarType.NAVAL },
            ['Band'] = {
                [true] = { 0.033310, 0.016655 },
                [false] = HOUND.DB.Bands.F
            },
            ['Primary'] = true,
            ['numDistinctFreqs'] = 0
        },

        ['Type_093'] = {
            ['Name'] = "Shang Submarine",
            ['Assigned'] = { "Naval" },
            ['Role'] = { HOUND.DB.RadarType.NAVAL },
            ['Band'] = {
                [true] = HOUND.DB.Bands.E,
                [false] = HOUND.DB.Bands.E
            },
            ['Primary'] = true,
            ['numDistinctFreqs'] = 0
        },
        ['USS_Arleigh_Burke_IIa'] = {
            ['Name'] = "Arleigh Burke (DD)",
            ['Assigned'] = { "Naval" },
            ['Role'] = { HOUND.DB.RadarType.NAVAL },
            ['Band'] = {
                [true] = HOUND.DB.Bands.I,
                [false] = HOUND.DB.Bands.I
            },
            ['Primary'] = true,
            ['numDistinctFreqs'] = 0
        },
        ['CV_1143_5'] = {
            ['Name'] = "Kuznetsov (CV)",
            ['Assigned'] = { "Naval" },
            ['Role'] = { HOUND.DB.RadarType.NAVAL },
            ['Band'] = {
                [true] = { 0.037474, 0.037474 },
                [false] = { 0.075325, 0.003052 }
            },
            ['Primary'] = true,
            ['numDistinctFreqs'] = 0
        },
        ['KUZNECOW'] = {
            ['Name'] = "Kuznetsov (CV)",
            ['Assigned'] = { "Naval" },
            ['Role'] = { HOUND.DB.RadarType.NAVAL },
            ['Band'] = {
                [true] = { 0.037474, 0.037474 },
                [false] = { 0.075325, 0.003052 }
            },
            ['Primary'] = true,
            ['numDistinctFreqs'] = 0
        },
        ['Forrestal'] = {
            ['Name'] = "Forrestal (CV)",
            ['Assigned'] = { "Naval" },
            ['Role'] = { HOUND.DB.RadarType.NAVAL },
            ['Band'] = {
                [true] = { 0.022472, 0.005789 },
                [false] = { 0.022472, 0.005789 },
            },
            ['Primary'] = true,
            ['numDistinctFreqs'] = 0
        },
        ['VINSON'] = {
            ['Name'] = "Nimitz (CV)",
            ['Assigned'] = { "Naval" },
            ['Role'] = { HOUND.DB.RadarType.NAVAL },
            ['Band'] = {
                [true] = { 0.318251, 0.034446 },
                [false] = { 0.318251, 0.034446 }
            },
            ['Primary'] = true,
            ['numDistinctFreqs'] = 0
        },
        ['CVN_71'] = {
            ['Name'] = "Nimitz (CV)",
            ['Assigned'] = { "Naval" },
            ['Role'] = { HOUND.DB.RadarType.NAVAL },
            ['Band'] = {
                [true] = { 0.024983, 0.012491 },
                [false] = HOUND.DB.Bands.I
            },
            ['Primary'] = true,
            ['numDistinctFreqs'] = 0
        },
        ['CVN_72'] = {
            ['Name'] = "Nimitz (CV)",
            ['Assigned'] = { "Naval" },
            ['Role'] = { HOUND.DB.RadarType.NAVAL },
            ['Band'] = {
                [true] = { 0.024983, 0.012491 },
                [false] = HOUND.DB.Bands.I
            },
            ['Primary'] = true,
            ['numDistinctFreqs'] = 0
        },
        ['CVN_73'] = {
            ['Name'] = "Nimitz (CV)",
            ['Assigned'] = { "Naval" },
            ['Role'] = { HOUND.DB.RadarType.NAVAL },
            ['Band'] = {
                [true] = { 0.024983, 0.012491 },
                [false] = HOUND.DB.Bands.I
            },
            ['Primary'] = true,
            ['numDistinctFreqs'] = 0
        },
        ['Stennis'] = {
            ['Name'] = "Nimitz (CV)",
            ['Assigned'] = { "Naval" },
            ['Role'] = { HOUND.DB.RadarType.NAVAL },
            ['Band'] = {
                [true] = { 0.024983, 0.012491 },
                [false] = HOUND.DB.Bands.I
            },
            ['Primary'] = true,
            ['numDistinctFreqs'] = 0
        },
        ['CVN_75'] = {
            ['Name'] = "Nimitz (CV)",
            ['Assigned'] = { "Naval" },
            ['Role'] = { HOUND.DB.RadarType.NAVAL },
            ['Band'] = {
                [true] = { 0.024983, 0.012491 },
                [false] = HOUND.DB.Bands.I
            },
            ['Primary'] = true,
            ['numDistinctFreqs'] = 0
        },
        ['La_Combattante_II'] = {
            ['Name'] = "La Combattante (FC)",
            ['Assigned'] = { "Naval" },
            ['Role'] = { HOUND.DB.RadarType.NAVAL },
            ['Band'] = {
                [true] = { 0.024983, 0.012491 },
                [false] = { 0.049965, 0.007687 }
            },
            ['Primary'] = true,
            ['numDistinctFreqs'] = 0
        },
        ['ALBATROS'] = {
            ['Name'] = "Grisha (FC)",
            ['Assigned'] = { "Naval" },
            ['Role'] = { HOUND.DB.RadarType.NAVAL },
            ['Band'] = {
                [true] = {0.020256,0.000856},
                [false] = {0.031624,0.000682}
            },
            ['Primary'] = true,
            ['numDistinctFreqs'] = 0
        },
        ['MOLNIYA'] = {
            ['Name'] = "Molniya (FC)",
            ['Assigned'] = { "Naval" },
            ['Role'] = { HOUND.DB.RadarType.NAVAL },
            ['Band'] = {
                [true] = { 0.031691, 0.000202 },
                [false] = { 0.031691, 0.000202 }
            },
            ['Primary'] = true,
            ['numDistinctFreqs'] = 0
        },
        ['MOSCOW'] = {
            ['Name'] = "Moskva (CG)",
            ['Assigned'] = { "Naval" },
            ['Role'] = { HOUND.DB.RadarType.NAVAL },
            ['Band'] = {
                [true] = { 0.024879, 0.012595 },
                [false] = HOUND.DB.Bands.H
            },
            ['Primary'] = true,
            ['numDistinctFreqs'] = 0
        },
        ['NEUSTRASH'] = {
            ['Name'] = "Neustrashimy (DD)",
            ['Assigned'] = { "Naval" },
            ['Role'] = { HOUND.DB.RadarType.NAVAL },
            ['Band'] = {
                [true] = { 0.037474, 0.037474 },
                [false] = { 0.031691, 0.000202 }
            },
            ['Primary'] = true,
            ['numDistinctFreqs'] = 0
        },
        ['PERRY'] = {
            ['Name'] = "Oliver H. Perry (FF)",
            ['Assigned'] = { "Naval" },
            ['Role'] = { HOUND.DB.RadarType.NAVAL },
            ['Band'] = {
                [true] = { 0.029682, 0.007329 },
                [false] = HOUND.DB.Bands.I
            },
            ['Primary'] = true,
            ['numDistinctFreqs'] = 0
        },
        ['PIOTR'] = {
            ['Name'] = "Kirov (CG)",
            ['Assigned'] = { "Naval" },
            ['Role'] = { HOUND.DB.RadarType.NAVAL },
            ['Band'] = {
                [true] = { 0.020256, 0.000856 },
                [false] = { 0.020256, 0.000856 }
            },
            ['Primary'] = true,
            ['numDistinctFreqs'] = 0
        },
        ['REZKY'] = {
            ['Name'] = "Krivak (FF)",
            ['Assigned'] = { "Naval" },
            ['Role'] = { HOUND.DB.RadarType.NAVAL },
            ['Band'] = {
                [true] = { 31757675.635593, 203140.782317 },
                [false] = { 31757675.635593, 203140.782317 }
            },
            ['Primary'] = true,
            ['numDistinctFreqs'] = 0
        },
        ['LHA_Tarawa'] = {
            ['Name'] = "Tarawa (LHA)",
            ['Assigned'] = { "Naval" },
            ['Role'] = { HOUND.DB.RadarType.NAVAL },
            ['Band'] = {
                [true] = { 0.516884, 0.082701 },
                [false] = { 0.516884, 0.082701 }
            },
            ['Primary'] = true,
            ['numDistinctFreqs'] = 0
        },
        ['TICONDEROG'] = {
            ['Name'] = "Ticonderoga (CG)",
            ['Assigned'] = { "Naval" },
            ['Role'] = { HOUND.DB.RadarType.NAVAL },
            ['Band'] = {
                [true] = HOUND.DB.Bands.I,
                [false] = HOUND.DB.Bands.I
            },
            ['Primary'] = true,
            ['numDistinctFreqs'] = 0
        },
        ['hms_invincible'] = {
            ['Name'] = "Invincible (CV)",
            ['Assigned'] = { "Naval" },
            ['Role'] = { HOUND.DB.RadarType.NAVAL },
            ['Band'] = {
                [true] = { 0.516884, 0.082701 },
                [false] = { 0.516884, 0.082701 }
            },
            ['Primary'] = true,
            ['numDistinctFreqs'] = 0
        },
        ['leander-gun-achilles'] = {
            ['Name'] = "Leander (FF)",
            ['Assigned'] = { "Naval" },
            ['Role'] = { HOUND.DB.RadarType.NAVAL },
            ['Band'] = {
                [true] = { 0.516884, 0.082701 },
                [false] = { 0.516884, 0.082701 }
            },
            ['Primary'] = true,
            ['numDistinctFreqs'] = 0
        },
        ['leander-gun-andromeda'] = {
            ['Name'] = "Leander (FF)",
            ['Assigned'] = { "Naval" },
            ['Role'] = { HOUND.DB.RadarType.NAVAL },
            ['Band'] = {
                [true] = { 0.516884, 0.082701 },
                [false] = { 0.516884, 0.082701 }
            },
            ['Primary'] = true,
            ['numDistinctFreqs'] = 0
        },
        ['leander-gun-ariadne'] = {
            ['Name'] = "Leander (FF)",
            ['Assigned'] = { "Naval" },
            ['Role'] = { HOUND.DB.RadarType.NAVAL },
            ['Band'] = {
                [true] = { 0.516884, 0.082701 },
                [false] = { 0.516884, 0.082701 }
            },
            ['Primary'] = true,
            ['numDistinctFreqs'] = 0
        },
        ['leander-gun-condell'] = {
            ['Name'] = "Condell (FF)",
            ['Assigned'] = { "Naval" },
            ['Role'] = { HOUND.DB.RadarType.NAVAL },
            ['Band'] = {
                [true] = { 0.516884, 0.082701 },
                [false] = { 0.516884, 0.082701 }
            },
            ['Primary'] = true,
            ['numDistinctFreqs'] = 0
        },
        ['leander-gun-lynch'] = {
            ['Name'] = "Condell (FF)",
            ['Assigned'] = { "Naval" },
            ['Role'] = { HOUND.DB.RadarType.NAVAL },
            ['Band'] = {
                [true] = { 0.516884, 0.082701 },
                [false] = { 0.516884, 0.082701 }
            },
            ['Primary'] = true,
            ['numDistinctFreqs'] = 0
        },
        ['ara_vdm'] = {
            ['Name'] = "Veinticinco de Mayo (CV)",
            ['Assigned'] = { "Naval" },
            ['Role'] = { HOUND.DB.RadarType.NAVAL },
            ['Band'] = {
                [true] = { 0.516884, 0.082701 },
                [false] = { 0.136269, 0.013627 }
            },
            ['Primary'] = true,
            ['numDistinctFreqs'] = 0
        },
        ['santafe'] = {
            ['Name'] = "Balao Class (SS)",
            ['Assigned'] = { "Naval" },
            ['Role'] = { HOUND.DB.RadarType.NAVAL },
            ['Band'] = {
                [true] = { 0.136269, 0.013627 },
                [false] = { 0.136269, 0.013627 }
            },
            ['Primary'] = true,
            ['numDistinctFreqs'] = 0
        },
        ['Essex'] = {
            ['Name'] = "Essex (CV)",
            ['Assigned'] = { "Naval" },
            ['Role'] = { HOUND.DB.RadarType.NAVAL },
            ['Band'] = {
                [true] = { 0.516884, 0.082701 },
                [false] = { 0.136269, 0.013627 }
            },
            ['Primary'] = true,
            ['numDistinctFreqs'] = 0
        },
        ['BDK-775'] = {
            ['Name'] = "Ropucha (LS)",
            ['Assigned'] = { "Naval" },
            ['Role'] = { HOUND.DB.RadarType.NAVAL },
            ['Band'] = {
                [true] = {0.031624,0.000682},
                [false] = {0.031624,0.000682}
            },
            ['Primary'] = true,
            ['numDistinctFreqs'] = 0
        },
        ['Type_071'] = {
            ['Name'] = "Yuzhao transport",
            ['Assigned'] = { "Naval" },
            ['Role'] = { HOUND.DB.RadarType.NAVAL },
            ['Band'] = {
                [true] = HOUND.DB.Bands.E,
                [false] = HOUND.DB.Bands.E
            },
            ['Primary'] = true,
            ['numDistinctFreqs'] = 0
        },
        ['Type_021_1'] = {
            ['Name'] = "Type 021-1 Missile Boat",
            ['Assigned'] = {"Naval"},
            ['Role'] = { HOUND.DB.RadarType.NAVAL },
            ['Band'] = {
                    [true] = {0.024983,0.012491},
                    [false] = {0.024983,0.012491}
            },
            ['Primary'] = true,
            ['numDistinctFreqs'] = 0
        },
        ['atconveyor'] = {
            ['Name'] = "SS Atlantic Conveyor",
            ['Assigned'] = { "Naval" },
            ['Role'] = { HOUND.DB.RadarType.NAVAL },
            ['Band'] = {
                [true] = HOUND.DB.Bands.D,
                [false] = HOUND.DB.Bands.D
            },
            ['Primary'] = true,
            ['numDistinctFreqs'] = 0
        },
    }

    HOUND.DB.Radars['CHAP_IRISTSLM_STR'] = {
        ['Name'] = "IRIS-T",
        ['Assigned'] = { "SHORAD" },
        ['Role'] = { HOUND.DB.RadarType.SEARCH, HOUND.DB.RadarType.TRACK },
        ['Band'] = {
            [true] = { 0.049965, 0.024983 },
            [false] = { 0.049965, 0.024983 }
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['CHAP_PantsirS1'] = {
        ['Name'] = "Pantsir",
        ['Assigned'] = { "SHORAD" },
        ['Role'] = { HOUND.DB.RadarType.SEARCH, HOUND.DB.RadarType.TRACK },
        ['Band'] = {
            [true] = { 0.016655, 0.020819 },
            [false] = { 0.074948, 0.074948 }
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['CHAP_TorM2'] = {
        ['Name'] = "Tor",
        ['Assigned'] = { "SA-15" },
        ['Role'] = { HOUND.DB.RadarType.SEARCH, HOUND.DB.RadarType.TRACK },
        ['Band'] = {
            [true] = { 0.011103, 0.005552 },
            [false] = { 0.074948, 0.074948 }
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['CHAP_Project22160'] = {
        ['Name'] = "Project 22160 (DD)",
        ['Assigned'] = { "Naval" },
        ['Role'] = { HOUND.DB.RadarType.NAVAL },
        ['Band'] = {
            [true] = { 0.024983, 0.012491 },
            [false] = { 0.024983, 0.012491 }
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['CHAP_Project22160_TorM2KM'] = {
        ['Name'] = "Project 22160 (DD)",
        ['Assigned'] = { "Naval" },
        ['Role'] = { HOUND.DB.RadarType.NAVAL },
        ['Band'] = {
            [true] = { 0.011103, 0.005552 },
            [false] = { 0.024983, 0.012491 }
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }

    HOUND.DB.Platform = {
        [Object.Category.STATIC] = {
            ['Comms tower M']   = { antenna = { size = 107, factor = 1 }, ins_error = 0 },
            ['.Command Center'] = { antenna = { size = 62, factor = 1 }, ins_error = 0 },
            ['Cow']             = { antenna = { size = 1000, factor = 10 }, ins_error = 0 },
            ['TV tower']        = { antenna = { size = 235, factor = 1 }, ins_error = 0 },
        },
        [Object.Category.UNIT] = {
            ['Patriot AMG'] = { antenna = { size = 15, factor = 1 }, ins_error = 0 },
            ['SPK-11'] = { antenna = { size = 15, factor = 1 }, ins_error = 0 },
            ['CH-47D'] = { antenna = { size = 12, factor = 1 }, ins_error = 0 },
            ['CH-53E'] = { antenna = { size = 10, factor = 1 }, ins_error = 0 },
            ['MIL-26'] = { antenna = { size = 20, factor = 1 }, ins_error = 50 },
            ['SH-60B'] = { antenna = { size = 8, factor = 1 }, ins_error = 0 },
            ['UH-60A'] = { antenna = { size = 8, factor = 1 }, ins_error = 0 },
            ['Mi-8MT'] = { antenna = { size = 8, factor = 1 }, ins_error = 0 },
            ['UH-1H'] = { antenna = { size = 4, factor = 1 }, ins_error = 50 },
            ['KA-27'] = { antenna = { size = 4, factor = 1 }, ins_error = 50 },
            ['C-130'] = { antenna = { size = 35, factor = 1 }, ins_error = 0 },
            ['C-130J-30'] = { antenna = { size = 35, factor = 1 }, ins_error = 0 },
            ['C-17A'] = { antenna = { size = 40, factor = 1 }, ins_error = 0 }, -- stand-in for RC-135, tuned antenna size to match
            ['S-3B'] = { antenna = { size = 18, factor = 0.8 }, ins_error = 0 },
            ['E-3A'] = { antenna = { size = 9, factor = 0.5 }, ins_error = 0 },
            ['E-2C'] = { antenna = { size = 7, factor = 0.5 }, ins_error = 0 },
            ['Tu-95MS'] = { antenna = { size = 50, factor = 1 }, ins_error = 50 },
            ['Tu-142'] = { antenna = { size = 50, factor = 1 }, ins_error = 0 },
            ['IL-76MD'] = { antenna = { size = 48, factor = 0.8 }, ins_error = 50 },
            ['H-6J'] = { antenna = { size = 3.5, factor = 1 }, require = { Payload = { 'PHANTASM' } }, ins_error = 100 },
            ['Su-24M'] = { antenna = { size = 3.5, factor = 1 }, require = { Payload = { 'PHANTASM' } }, ins_error = 50 },
            ['Su-24MR'] = { antenna = { size = 4.5, factor = 1 }, require = { Payload = { 'TANGAZH' } }, ins_error = 50 },
            ['Su-25TM'] = { antenna = { size = 3.5, factor = 1 }, require = { Payload = { 'PHANTASM' } }, ins_error = 50 },
            ['An-30M'] = { antenna = { size = 25, factor = 1 }, ins_error = 50 },
            ['A-50'] = { antenna = { size = 9, factor = 0.5 }, ins_error = 0 },
            ['An-26B'] = { antenna = { size = 26, factor = 1 }, ins_error = 100 },
            ['C-47'] = { antenna = { size = 12, factor = 1 }, ins_error = 100 },
            ['Su-25T'] = { antenna = { size = 3.5, factor = 1 }, require = { Payload = { 'PHANTASM' } }, ins_error = 50 },
            ['AJS37'] = { antenna = { size = 4.5, factor = 1 }, require = { Payload = { 'U22/A Jammer Pod', 'U22 Jammer' } }, ins_error = 50 },
            ['F-16C_50'] = { antenna = { size = 1.45, factor = 1 }, require = { Payload = { 'f-16c_hts_pod' } }, ins_error = 0 },
            ['JF-17'] = { antenna = { size = 3.25, factor = 1 }, require = { Payload = { 'KG-600' } }, ins_error = 0 },
            ['A6E'] = { antenna = { size = 9, factor = 1 }, require = { Payload = { 'AN/ALQ-99','HB_F14_EXT_AN_APQ-167','ALQ167' } }, ins_error = 0 }, -- A-6E stand in for EA-6B, ALQ167 is Standin for ALQ-99
            ['Mirage-F1EE'] = { antenna = { size = 3.7, factor = 1 }, require = { Payload = { 'TMV_018_Syrel_POD' } }, ins_error = 50 }, -- does not reflect features in actual released product
            ['Mirage-F1M-CE'] = { antenna = { size = 3.7, factor = 1 }, require = { Payload = { 'TMV_018_Syrel_POD' } }, ins_error = 0 }, -- does not reflect features in actual released product
            ['Mirage-F1M-EE'] = { antenna = { size = 3.7, factor = 1 }, require = { Payload = { 'TMV_018_Syrel_POD' } }, ins_error = 0 }, -- does not reflect features in actual released product
            ['Mirage-F1CR'] = { antenna = { size = 4, factor = 1 }, require = { Payload = { 'ASTAC_POD' } }, ins_error = 0 },           -- AI only (FAF)
            ['Mirage-F1EQ'] = { antenna = { size = 3.7, factor = 1 }, require = { Payload = { 'TMV_018_Syrel_POD' } }, ins_error = 50 }, -- AI only (Iraq)
            ['Mirage-F1EDA'] = { antenna = { size = 3.7, factor = 1 }, require = { Payload = { 'TMV_018_Syrel_POD' } }, ins_error = 50 }, -- AI only (Qatar)
        }
    }
end
do
    HOUND.DB.Platform[Object.Category.UNIT]['UH-60L'] = { antenna = { size = 8, factor = 1 }, ins_error = 0 }      -- community UH-69L
    HOUND.DB.Platform[Object.Category.UNIT]['Hercules'] = { antenna = { size = 35, factor = 1 }, ins_error = 0 }   -- Anubis' C-130J
    HOUND.DB.Platform[Object.Category.UNIT]['EC130'] = { antenna = { size = 35, factor = 1 }, ins_error = 0 }      -- Secret Squirrel EC-130
    HOUND.DB.Platform[Object.Category.UNIT]['RC135RJ'] = { antenna = { size = 40, factor = 1 }, ins_error = 0 }    -- Secret Squirrel RC-135
    HOUND.DB.Platform[Object.Category.UNIT]['P3C_Orion'] = { antenna = { size = 25, factor = 1 }, ins_error = 0 }  -- MAM P-3C_Orion
    HOUND.DB.Platform[Object.Category.UNIT]['CLP_P8'] = { antenna = { size = 35, factor = 1 }, ins_error = 0 }     -- CLP P-8A posidon
    HOUND.DB.Platform[Object.Category.UNIT]['CLP_TU214R'] = { antenna = { size = 40, factor = 1 }, ins_error = 0 } -- CLP TU-214R
    HOUND.DB.Platform[Object.Category.UNIT]['EA_6B'] = { antenna = { size = 9, factor = 1 }, ins_error = 0 }       --VSN EA-6B
    HOUND.DB.Platform[Object.Category.UNIT]['EA-18G'] = { antenna = { size = 14, factor = 1 }, ins_error = 0 }     --CJS EF-18G
    HOUND.DB.Platform[Object.Category.UNIT]['Shavit'] = { antenna = { size = 30, factor = 1 }, ins_error = 0 }     --IDF_Mods Shavit
    HOUND.DB.Platform[Object.Category.UNIT]['SU22'] = { antenna = { size = 3.5, factor = 1 }, require = { Payload = { 'SU22_BA52' } }, ins_error = 50 } --VinntoreZ SU-22M4 Mod
    HOUND.DB.Platform[Object.Category.UNIT]['SU22_AI'] = { antenna = { size = 3.5, factor = 1 }, require = { Payload = { 'SU22_BA52' } }, ins_error = 50 } --VinntoreZ SU-22M4 Mod

    HOUND.DB.Radars['S-300PS 64H6E TRAILER sr'] = {
        ['Name'] = "Big Bird",
        ['Assigned'] = { "SA-10" },
        ['Role'] = { HOUND.DB.RadarType.SEARCH },
        ['Band'] = {
            [true] = HOUND.DB.Bands.C,
            [false] = HOUND.DB.Bands.C
        },
        ['Primary'] = false,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['S-300PS SA-10B 40B6MD MAST sr'] = {
        ['Name'] = "Clam Shell",
        ['Assigned'] = { "SA-10" },
        ['Role'] = { HOUND.DB.RadarType.SEARCH },
        ['Band'] = {
            [true] = HOUND.DB.Bands.I,
            [false] = HOUND.DB.Bands.I
        },
        ['Primary'] = false,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['S-300PS 40B6M MAST tr'] = {
        ['Name'] = "Flap Lid",
        ['Assigned'] = { "SA-10" },
        ['Role'] = { HOUND.DB.RadarType.TRACK },
        ['Band'] = {
            [true] = HOUND.DB.Bands.J,
            [false] = HOUND.DB.Bands.J
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['S-300PS 30H6 TRAILER tr'] = {
        ['Name'] = "Flap Lid",
        ['Assigned'] = { "SA-10" },
        ['Role'] = { HOUND.DB.RadarType.TRACK },
        ['Band'] = {
            [true] = HOUND.DB.Bands.J,
            [false] = HOUND.DB.Bands.J
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['S-300PS 30N6 TRAILER tr'] = {
        ['Name'] = "Flap Lid",
        ['Assigned'] = { "SA-10" },
        ['Role'] = { HOUND.DB.RadarType.TRACK },
        ['Band'] = {
            [true] = HOUND.DB.Bands.J,
            [false] = HOUND.DB.Bands.J
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['S-300PMU1 40B6MD sr'] = {
        ['Name'] = "Clam Shell",
        ['Assigned'] = { "SA-10" },
        ['Role'] = { HOUND.DB.RadarType.SEARCH },
        ['Band'] = {
            [true] = HOUND.DB.Bands.I,
            [false] = HOUND.DB.Bands.I
        },
        ['Primary'] = false,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['S-300PMU1 64N6E sr'] = {
        ['Name'] = "Big Bird",
        ['Assigned'] = { "SA-10" },
        ['Role'] = { HOUND.DB.RadarType.SEARCH },
        ['Band'] = {
            [true] = HOUND.DB.Bands.C,
            [false] = HOUND.DB.Bands.C
        },
        ['Primary'] = false,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['S-300PMU1 30N6E tr'] = {
        ['Name'] = "Flap Lid",
        ['Assigned'] = { "SA-10" },
        ['Role'] = { HOUND.DB.RadarType.TRACK },
        ['Band'] = {
            [true] = HOUND.DB.Bands.J,
            [false] = HOUND.DB.Bands.J
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['S-300PMU1 40B6M tr'] = {
        ['Name'] = "Grave Stone",
        ['Assigned'] = { "SA-10" },
        ['Role'] = { HOUND.DB.RadarType.TRACK },
        ['Band'] = {
            [true] = HOUND.DB.Bands.J,
            [false] = HOUND.DB.Bands.J
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['S-300V 9S15 sr'] = {
        ['Name'] = 'Bill Board',
        ['Assigned'] = { "SA-10" },
        ['Role'] = { HOUND.DB.RadarType.SEARCH },
        ['Band'] = {
            [true] = HOUND.DB.Bands.E,
            [false] = HOUND.DB.Bands.E
        },
        ['Primary'] = false,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['S-300V 9S19 sr'] = {
        ['Name'] = 'High Screen',
        ['Assigned'] = { "SA-10" },
        ['Role'] = { HOUND.DB.RadarType.SEARCH },
        ['Band'] = {
            [true] = HOUND.DB.Bands.C,
            [false] = HOUND.DB.Bands.C
        },
        ['Primary'] = false,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['S-300V 9S32 tr'] = {
        ['Name'] = 'Grill Pan',
        ['Assigned'] = { "SA-12" },
        ['Role'] = { HOUND.DB.RadarType.TRACK },
        ['Band'] = {
            [true] = HOUND.DB.Bands.J,
            [false] = HOUND.DB.Bands.J
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['S-300PMU2 92H6E tr'] = {
        ['Name'] = 'Grave Stone',
        ['Assigned'] = { "SA-10" },
        ['Role'] = { HOUND.DB.RadarType.TRACK },
        ['Band'] = {
            [true] = HOUND.DB.Bands.I,
            [false] = HOUND.DB.Bands.I
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['S-300PMU2 64H6E2 sr'] = {
        ['Name'] = "Big Bird",
        ['Assigned'] = { "SA-10" },
        ['Role'] = { HOUND.DB.RadarType.SEARCH },
        ['Band'] = {
            [true] = HOUND.DB.Bands.C,
            [false] = HOUND.DB.Bands.C
        },
        ['Primary'] = false,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['S-300VM 9S15M2 sr'] = {
        ['Name'] = 'Bill Board M',
        ['Assigned'] = { "SA-10" },
        ['Role'] = { HOUND.DB.RadarType.SEARCH },
        ['Band'] = {
            [true] = HOUND.DB.Bands.E,
            [false] = HOUND.DB.Bands.E
        },
        ['Primary'] = false,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['S-300VM 9S19M2 sr'] = {
        ['Name'] = 'High Screen M',
        ['Assigned'] = { "SA-10" },
        ['Role'] = { HOUND.DB.RadarType.SEARCH },
        ['Band'] = {
            [true] = HOUND.DB.Bands.C,
            [false] = HOUND.DB.Bands.C
        },
        ['Primary'] = false,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['S-300VM 9S32ME tr'] = {
        ['Name'] = 'Grill Pan M',
        ['Assigned'] = { "SA-12" },
        ['Role'] = { HOUND.DB.RadarType.TRACK },
        ['Band'] = {
            [true] = HOUND.DB.Bands.K,
            [false] = HOUND.DB.Bands.K
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['SA-17 Buk M1-2 LN 9A310M1-2'] = {
        ['Name'] = "Fire Dome M",
        ['Assigned'] = { "SA-11" },
        ['Role'] = { HOUND.DB.RadarType.TRACK },
        ['Band'] = {
            [true] = HOUND.DB.Bands.H,
            [false] = HOUND.DB.Bands.H
        },
        ['Primary'] = false,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['34Ya6E Gazetchik E decoy'] = {
        ['Name'] = "Flap Lid",
        ['Assigned'] = { "SA-10" },
        ['Role'] = { HOUND.DB.RadarType.TRACK },
        ['Band'] = {
            [true] = HOUND.DB.Bands.J,
            [false] = HOUND.DB.Bands.J
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['SAMPT_MRI_ARABEL'] = {
        ['Name'] = "SAMP/T",
        ['Assigned'] = { "SAMP/T" },
        ['Role'] = { HOUND.DB.RadarType.SEARCH, HOUND.DB.RadarType.TRACK },
        ['Band'] = {
            [true] = HOUND.DB.Bands.I,
            [false] = HOUND.DB.Bands.I
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['SAMPT_MRI_GF300'] = {
        ['Name'] = "SAMP/T",
        ['Assigned'] = { "SAMP/T" },
        ['Role'] = { HOUND.DB.RadarType.SEARCH, HOUND.DB.RadarType.TRACK },
        ['Band'] = {
            [true] = HOUND.DB.Bands.K,
            [false] = HOUND.DB.Bands.K
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['Fire Can radar'] = {
        ['Name'] = "Fire Can",
        ['Assigned'] = { "AAA" },
        ['Role'] = { HOUND.DB.RadarType.TRACK },
        ['Band'] = {
            [true] = HOUND.DB.Bands.E,
            [false] = HOUND.DB.Bands.E
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['EWR 55G6U NEBO-U'] = {
        ['Name'] = "Tall Rack",
        ['Assigned'] = { "EWR" },
        ['Role'] = { HOUND.DB.RadarType.EWR },
        ['Band'] = {
            [true] = HOUND.DB.Bands.A,
            [false] = HOUND.DB.Bands.A
        },
        ['Primary'] = false,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['EWR P-37 BAR LOCK'] = {
        ['Name'] = "Bar lock",
        ['Assigned'] = { "EWR", "SA-5" },
        ['Role'] = { HOUND.DB.RadarType.EWR, HOUND.DB.RadarType.SEARCH },
        ['Band'] = {
            [true] = HOUND.DB.Bands.E,
            [false] = HOUND.DB.Bands.E
        },
        ['Primary'] = false,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['EWR 1L119 Nebo-SVU'] = {
        ['Name'] = "Box Spring",
        ['Assigned'] = { "EWR" },
        ['Role'] = { HOUND.DB.RadarType.EWR },
        ['Band'] = {
            [true] = HOUND.DB.Bands.A,
            [false] = HOUND.DB.Bands.A
        },
        ['Primary'] = false,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['EWR Generic radar tower'] = {
        ['Name'] = "Civilian Radar",
        ['Assigned'] = { "EWR" },
        ['Role'] = { HOUND.DB.RadarType.EWR },
        ['Band'] = {
            [true] = HOUND.DB.Bands.C,
            [false] = HOUND.DB.Bands.C
        },
        ['Primary'] = false,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['PantsirS1'] = {
        ['Name'] = "Pantsir",
        ['Assigned'] = { "SA-22" },
        ['Role'] = { HOUND.DB.RadarType.SEARCH, HOUND.DB.RadarType.TRACK },
        ['Band'] = {
            [true] = { 0.516884, 0.082701 },
            [false] = { 0.136269, 0.013627 }
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['PantsirS2'] = {
        ['Name'] = "Pantsir",
        ['Assigned'] = { "SA-22" },
        ['Role'] = { HOUND.DB.RadarType.SEARCH, HOUND.DB.RadarType.TRACK },
        ['Band'] = {
            [true] = { 0.516884, 0.082701 },
            [false] = { 0.136269, 0.013627 }
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['Admiral_Kasatonov'] = {
        ['Name'] = "Gorshkov (FF)",
        ['Assigned'] = { "Naval" },
        ['Role'] = { HOUND.DB.RadarType.NAVAL },
        ['Band'] = {
            [true] = HOUND.DB.Bands.F,
            [false] = HOUND.DB.Bands.F
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['Karakurt_AShM'] = {
        ['Name'] = "Karakurt (FS)",
        ['Assigned'] = { "Naval" },
        ['Role'] = { HOUND.DB.RadarType.NAVAL },
        ['Band'] = {
            [true] = { 0.516884, 0.082701 },
            [false] = { 0.136269, 0.013627 }
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['Karakurt_LACM'] = {
        ['Name'] = "Karakurt (FS)",
        ['Assigned'] = { "Naval" },
        ['Role'] = { HOUND.DB.RadarType.NAVAL },
        ['Band'] = {
            [true] = { 0.516884, 0.082701 },
            [false] = { 0.136269, 0.013627 }
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['MonolitB'] = {
        ['Name'] = "Monolit B",
        ['Assigned'] = { "Bastion" },
        ['Role'] = { HOUND.DB.RadarType.ANTISHIP },
        ['Band'] = {
            [true] = HOUND.DB.Bands.I,
            [false] = HOUND.DB.Bands.I
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['TorM2'] = {
        ['Name'] = "Tor",
        ['Assigned'] = { "SA-15" },
        ['Role'] = { HOUND.DB.RadarType.SEARCH, HOUND.DB.RadarType.TRACK },
        ['Band'] = {
            [true] = { 0.516884, 0.082701 },
            [false] = { 0.136269, 0.013627 }
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['TorM2K'] = {
        ['Name'] = "Tor",
        ['Assigned'] = { "SA-15" },
        ['Role'] = { HOUND.DB.RadarType.SEARCH, HOUND.DB.RadarType.TRACK },
        ['Band'] = {
            [true] = { 0.516884, 0.082701 },
            [false] = { 0.136269, 0.013627 }
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['TorM2M'] = {
        ['Name'] = "Tor",
        ['Assigned'] = { "SA-15" },
        ['Role'] = { HOUND.DB.RadarType.SEARCH, HOUND.DB.RadarType.TRACK },
        ['Band'] = {
            [true] = { 0.516884, 0.082701 },
            [false] = { 0.136269, 0.013627 }
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }

    HOUND.DB.Radars['CH_BukM3_9A317M'] = {
        ['Name'] = "Fire Dome",
        ['Assigned'] = { "SA-11" },
        ['Role'] = { HOUND.DB.RadarType.TRACK },
        ['Band'] = {
            [true] = { 0.136269, 0.013627 },
            [false] = { 0.136269, 0.013627 }
        },
        ['Primary'] = false,
        ['numDistinctFreqs'] = 0

    }
    HOUND.DB.Radars['CH_BukM3_9A317MA'] = {
        ['Name'] = "Fire Dome",
        ['Assigned'] = { "SA-11" },
        ['Role'] = { HOUND.DB.RadarType.TRACK },
        ['Band'] = {
            [true] = { 0.136269, 0.013627 },
            [false] = { 0.136269, 0.013627 }
        },
        ['Primary'] = false,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['CH_BukM3_9S18M13'] = {
        ['Name'] = "Snow Drift",
        ['Assigned'] = { "SA-11", "SA-17", "SA-27" },
        ['Role'] = { HOUND.DB.RadarType.SEARCH },
        ['Band'] = {
            [true] = { 0.516884, 0.082701 },
            [false] = { 0.516884, 0.082701 }
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['CH_BukM3_9S36M'] = {
        ['Name'] = "Buk M3",
        ['Assigned'] = { "SA-27" },
        ['Role'] = { HOUND.DB.RadarType.TRACK },
        ['Band'] = {
            [true] = { 0.516884, 0.082701 },
            [false] = { 0.136269, 0.013627 }
        },
        ['Primary'] = false,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['CH_S350_50N6'] = {
        ['Name'] = "S-350 STR",
        ['Assigned'] = { "SA-25" },
        ['Role'] = { HOUND.DB.RadarType.SEARCH, HOUND.DB.RadarType.TRACK },
        ['Band'] = {
            [true] = { 0.516884, 0.082701 },
            [false] = { 0.136269, 0.013627 }
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0

    }
    HOUND.DB.Radars['CH_S350_96L6'] = {
        ['Name'] = "Cheese Board",
        ['Assigned'] = { "SA-10", "SA-23", "SA-25" },
        ['Role'] = { HOUND.DB.RadarType.SEARCH },
        ['Band'] = {
            [true] = { 0.516884, 0.082701 },
            [false] = { 0.516884, 0.082701 }
        },
        ['Primary'] = false,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['CH_Gremyashchiy_AShM'] = {
        ['Name'] = "Gremyashchiy Corvette",
        ['Assigned'] = { "Naval" },
        ['Role'] = { HOUND.DB.RadarType.NAVAL },
        ['Band'] = {
            [true] = { 0.516884, 0.082701 },
            [false] = { 0.136269, 0.013627 }
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['CH_Gremyashchiy_LACM'] = {
        ['Name'] = "Gremyashchiy Corvette",
        ['Assigned'] = { "Naval" },
        ['Role'] = { HOUND.DB.RadarType.NAVAL },
        ['Band'] = {
            [true] = { 0.516884, 0.082701 },
            [false] = { 0.136269, 0.013627 }
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['CH_Grigorovich_AShM'] = {
        ['Name'] = "Krivak 5 (FF)",
        ['Assigned'] = { "Naval" },
        ['Role'] = { HOUND.DB.RadarType.NAVAL },
        ['Band'] = {
            [true] = { 0.516884, 0.082701 },
            [false] = { 0.136269, 0.013627 }
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['CH_Grigorovich_LACM'] = {
        ['Name'] = "Krivak 5 (FF)",
        ['Assigned'] = { "Naval" },
        ['Role'] = { HOUND.DB.RadarType.NAVAL },
        ['Band'] = {
            [true] = { 0.516884, 0.082701 },
            [false] = { 0.136269, 0.013627 }
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['CH_Project22160'] = {
        ['Name'] = "Project 22160 (DD)",
        ['Assigned'] = { "Naval" },
        ['Role'] = { HOUND.DB.RadarType.NAVAL },
        ['Band'] = {
            [true] = { 0.136269, 0.013627 },
            [false] = { 0.136269, 0.013627 }
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0

    }
    HOUND.DB.Radars['CH_Steregushchiy'] = {
        ['Name'] = "Steregushchiy (FC)",
        ['Assigned'] = { "Naval" },
        ['Role'] = { HOUND.DB.RadarType.NAVAL },
        ['Band'] = {
            [true] = { 0.516884, 0.082701 },
            [false] = { 0.136269, 0.013627 }
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0

    }
    HOUND.DB.Radars['Admiral_Gorshkov'] = {
        ['Name'] = "Admiral Gorshkov (FF)",
        ['Assigned'] = { "Naval" },
        ['Role'] = { HOUND.DB.RadarType.NAVAL },
        ['Band'] = {
            [true] = { 0.516884, 0.082701 },
            [false] = { 0.136269, 0.013627 }
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }

    HOUND.DB.Radars['CH_Arleigh_Burke_IIA'] = {
        ['Name'] = "Arleigh Burke (DD)",
        ['Assigned'] = { "Naval" },
        ['Role'] = { HOUND.DB.RadarType.NAVAL },
        ['Band'] = {
            [true] = { 0.516884, 0.082701 },
            [false] = { 0.136269, 0.013627 }
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['CH_Arleigh_Burke_III'] = {
        ['Name'] = "Arleigh Burke (DD)",
        ['Assigned'] = { "Naval" },
        ['Role'] = { HOUND.DB.RadarType.NAVAL },

        ['Band'] = {
            [true] = { 0.516884, 0.082701 },
            [false] = { 0.136269, 0.013627 }
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['CH_Constellation'] = {
        ['Name'] = "[CH] Constellation Frigate",
        ['Assigned'] = { "Naval" },
        ['Role'] = { HOUND.DB.RadarType.NAVAL },
        ['Band'] = {
            [true] = { 0.516884, 0.082701 },
            [false] = { 0.136269, 0.013627 }
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['CH_Ticonderoga'] = {
        ['Name'] = "Ticonderoga (CG)",
        ['Assigned'] = { "Naval" },
        ['Role'] = { HOUND.DB.RadarType.NAVAL },
        ['Band'] = {
            [true] = { 0.516884, 0.082701 },
            [false] = { 0.136269, 0.013627 }
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['CH_Ticonderoga_CMP'] = {
        ['Name'] = "Ticonderoga (CG)",
        ['Assigned'] = { "Naval" },
        ['Role'] = { HOUND.DB.RadarType.NAVAL },
        ['Band'] = {
            [true] = { 0.516884, 0.082701 },
            [false] = { 0.136269, 0.013627 }
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['Arleigh_Burke_Flight_III_AShM'] = {
        ['Name'] = "Arleigh Burke (DD)",
        ['Assigned'] = { "Naval" },
        ['Role'] = { HOUND.DB.RadarType.NAVAL },
        ['Band'] = {
            [true] = HOUND.DB.Bands.E,
            [false] = HOUND.DB.Bands.E
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['Arleigh_Burke_Flight_III_LACM'] = {
        ['Name'] = "Arleigh Burke (DD)",
        ['Assigned'] = { "Naval" },
        ['Role'] = { HOUND.DB.RadarType.NAVAL },
        ['Band'] = {
            [true] = HOUND.DB.Bands.E,
            [false] = HOUND.DB.Bands.E
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['Arleigh_Burke_Flight_III_SAM'] = {
        ['Name'] = "Arleigh Burke (DD)",
        ['Assigned'] = { "Naval" },
        ['Role'] = { HOUND.DB.RadarType.NAVAL },
        ['Band'] = {
            [true] = HOUND.DB.Bands.E,
            [false] = HOUND.DB.Bands.E
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['Ticonderoga_CMP_AShM'] = {
        ['Name'] = "Ticonderoga (CG)",
        ['Assigned'] = { "Naval" },
        ['Role'] = { HOUND.DB.RadarType.NAVAL },
        ['Band'] = {
            [true] = HOUND.DB.Bands.E,
            [false] = HOUND.DB.Bands.E
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['Ticonderoga_CMP_LACM'] = {
        ['Name'] = "Ticonderoga (CG)",
        ['Assigned'] = { "Naval" },
        ['Role'] = { HOUND.DB.RadarType.NAVAL },
        ['Band'] = {
            [true] = HOUND.DB.Bands.E,
            [false] = HOUND.DB.Bands.E
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['Ticonderoga_CMP_SAM'] = {
        ['Name'] = "Ticonderoga (CG)",
        ['Assigned'] = { "Naval" },
        ['Role'] = { HOUND.DB.RadarType.NAVAL },
        ['Band'] = {
            [true] = HOUND.DB.Bands.E,
            [false] = HOUND.DB.Bands.E
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['MIM104_ANMPQ65'] = {
        ['Name'] = "Patriot",
        ['Assigned'] = { "Patriot" },
        ['Role'] = { HOUND.DB.RadarType.SEARCH, HOUND.DB.RadarType.TRACK },
        ['Band'] = {
            [true] = { 0.516884, 0.082701 },
            [false] = { 0.136269, 0.013627 }
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['MIM104_ANMPQ65A'] = {
        ['Name'] = "Patriot",
        ['Assigned'] = { "Patriot" },
        ['Role'] = { HOUND.DB.RadarType.SEARCH, HOUND.DB.RadarType.TRACK },
        ['Band'] = {
            [true] = { 0.516884, 0.082701 },
            [false] = { 0.136269, 0.013627 }
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['MIM104_LTAMDS'] = {
        ['Name'] = "Patriot LTAMDS",
        ['Assigned'] = { "Patriot" },
        ['Role'] = { HOUND.DB.RadarType.SEARCH, HOUND.DB.RadarType.TRACK },
        ['Band'] = {
            [true] = { 0.516884, 0.082701 },
            [false] = { 0.136269, 0.013627 }
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['MIM104_ANMPQ65A_HEMTT'] = {
        ['Name'] = "Patriot",
        ['Assigned'] = { "Patriot" },
        ['Role'] = { HOUND.DB.RadarType.SEARCH, HOUND.DB.RadarType.TRACK },
        ['Band'] = {
            [true] = { 0.516884, 0.082701 },
            [false] = { 0.136269, 0.013627 }
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['MIM104_ANMPQ65_HEMTT'] = {
        ['Name'] = "Patriot",
        ['Assigned'] = { "Patriot" },
        ['Role'] = { HOUND.DB.RadarType.SEARCH, HOUND.DB.RadarType.TRACK },
        ['Band'] = {
            [true] = { 0.516884, 0.082701 },
            [false] = { 0.136269, 0.013627 }
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }

    HOUND.DB.Radars['MIM104_LTAMDS_HEMTT'] = {
        ['Name'] = "Patriot LTAMDS",
        ['Assigned'] = { "Patriot" },
        ['Role'] = { HOUND.DB.RadarType.SEARCH, HOUND.DB.RadarType.TRACK },
        ['Band'] = {
            [true] = { 0.516884, 0.082701 },
            [false] = { 0.136269, 0.013627 }
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }

    HOUND.DB.Radars['CH_NASAMS3_SR'] = {
        ['Name'] = "Sentinel",
        ['Assigned'] = { "NASAMS" },
        ['Role'] = { HOUND.DB.RadarType.SEARCH },
        ['Band'] = {
            [true] = { 0.516884, 0.082701 },
            [false] = { 0.136269, 0.013627 }
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['CH_Centurion_C_RAM'] = {
        ['Name'] = "Centurion C-RAM",
        ['Assigned'] = { "AAA" },
        ['Role'] = { HOUND.DB.RadarType.TRACK },
        ['Band'] = {
            [true] = { 0.136269, 0.013627 },
            [false] = { 0.136269, 0.013627 }
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }

    HOUND.DB.Radars['CH_THAAD_ANTPY2'] = {
        ['Name'] = "THAAD STR",
        ['Assigned'] = { "THAAD" },
        ['Role'] = { HOUND.DB.RadarType.SEARCH, HOUND.DB.RadarType.TRACK },
        ['Band'] = {
            [true] = { 0.516884, 0.082701 },
            [false] = { 0.136269, 0.013627 }
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }

    HOUND.DB.Radars['Type45'] = {
        ['Name'] = "Type 45 (DD)",
        ['Assigned'] = { "Naval" },
        ['Role'] = { HOUND.DB.RadarType.NAVAL },
        ['Band'] = {
            [true] = { 0.516884, 0.082701 },
            [false] = { 0.136269, 0.013627 }
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['CH_Type26'] = {
        ['Name'] = "Type 26 (FF)",
        ['Assigned'] = { "Naval" },
        ['Role'] = { HOUND.DB.RadarType.NAVAL },
        ['Band'] = {
            [true] = { 0.516884, 0.082701 },
            [false] = { 0.136269, 0.013627 }
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['CH_SkySabreGiraffe'] = {
        ['Name'] = "Giraffe",
        ['Assigned'] = { "Sky Sabre" },
        ['Role'] = { HOUND.DB.RadarType.SEARCH, HOUND.DB.RadarType.TRACK },
        ['Band'] = {
            [true] = { 0.516884, 0.082701 },
            [false] = { 0.136269, 0.013627 }
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['HSwMS_Visby'] = {
        ['Name'] = "Visby (FS)",
        ['Assigned'] = { "Naval" },
        ['Role'] = { HOUND.DB.RadarType.NAVAL },
        ['Band'] = {
            [true] = { 0.516884, 0.082701 },
            [false] = { 0.136269, 0.013627 }
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['LvKv9040'] = {
        ['Name'] = "LvKv9040",
        ['Assigned'] = { "AAA" },
        ['Role'] = { HOUND.DB.RadarType.RANGEFINDER },
        ['Band'] = {
            [true] = { 0.136269, 0.013627 },
            [false] = { 0.136269, 0.013627 }
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['LvS-103_PM103'] = {
        ['Name'] = "Patriot",
        ['Assigned'] = { "Patriot" },
        ['Role'] = { HOUND.DB.RadarType.SEARCH, HOUND.DB.RadarType.TRACK },
        ['Band'] = {
            [true] = { 0.516884, 0.082701 },
            [false] = { 0.136269, 0.013627 }
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['LvS-103_PM103_HX'] = {
        ['Name'] = "Patriot",
        ['Assigned'] = { "Patriot" },
        ['Role'] = { HOUND.DB.RadarType.SEARCH, HOUND.DB.RadarType.TRACK },
        ['Band'] = {
            [true] = { 0.516884, 0.082701 },
            [false] = { 0.136269, 0.013627 }
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['RBS-90'] = {
        ['Name'] = "RBS-90",
        ['Assigned'] = { "SHORAD" },
        ['Role'] = { HOUND.DB.RadarType.RANGEFINDER },
        ['Band'] = {
            [true] = HOUND.DB.Bands.J,
            [false] = HOUND.DB.Bands.J
        },
        ['Primary'] = false,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['BV410_RBS90'] = {
        ['Name'] = "RBS-90",
        ['Assigned'] = { "SHORAD" },
        ['Role'] = { HOUND.DB.RadarType.RANGEFINDER },
        ['Band'] = {
            [true] = HOUND.DB.Bands.J,
            [false] = HOUND.DB.Bands.J
        },
        ['Primary'] = false,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['UndE23'] = {
        ['Name'] = "UndE23",
        ['Assigned'] = { "SHORAD" },
        ['Role'] = { HOUND.DB.RadarType.SEARCH, HOUND.DB.RadarType.TRACK },
        ['Band'] = {
            [true] = HOUND.DB.Bands.G,
            [false] = HOUND.DB.Bands.G
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }

    HOUND.DB.Radars['Strb90'] = {
        ['Name'] = "Strb 90 FAC",
        ['Assigned'] = { "Naval" },
        ['Role'] = { HOUND.DB.RadarType.NAVAL },
        ['Band'] = {
            [true] = HOUND.DB.Bands.E,
            [false] = HOUND.DB.Bands.E
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['CH_Type022'] = {
        ['Name'] = "Type 022 FAC",
        ['Assigned'] = { "Naval" },
        ['Role'] = { HOUND.DB.RadarType.NAVAL },
        ['Band'] = {
            [true] = { 0.136269, 0.013627 },
            [false] = { 0.136269, 0.013627 }
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['CH_Type054B'] = {
        ['Name'] = "Type 054B Frigate",
        ['Assigned'] = { "Naval" },
        ['Role'] = { HOUND.DB.RadarType.NAVAL },
        ['Band'] = {
            [true] = { 0.516884, 0.082701 },
            [false] = { 0.136269, 0.013627 }
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['CH_Type056A'] = {
        ['Name'] = "Type 056A Corvette",
        ['Assigned'] = { "Naval" },
        ['Role'] = { HOUND.DB.RadarType.NAVAL },
        ['Band'] = {
            [true] = { 0.516884, 0.082701 },
            [false] = { 0.136269, 0.013627 }
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['Type055'] = {
        ['Name'] = "Type 055 (CG)",
        ['Assigned'] = { "Naval" },
        ['Role'] = { HOUND.DB.RadarType.NAVAL },
        ['Band'] = {
            [true] = { 0.516884, 0.082701 },
            [false] = { 0.136269, 0.013627 }
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['Type052D'] = {
        ['Name'] = "Type 052D (DD)",
        ['Assigned'] = { "Naval" },
        ['Role'] = { HOUND.DB.RadarType.NAVAL },
        ['Band'] = {
            [true] = { 0.516884, 0.082701 },
            [false] = { 0.136269, 0.013627 }
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['PGL_625'] = {
        ['Name'] = "PGL-625",
        ['Assigned'] = { "SHORAD" },
        ['Role'] = { HOUND.DB.RadarType.SEARCH, HOUND.DB.RadarType.TRACK },
        ['Band'] = {
            [true] = { 0.136269, 0.013627 },
            [false] = { 0.136269, 0.013627 }
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['HQ17A'] = {
        ['Name'] = "HQ-17",
        ['Assigned'] = { "HQ-17" },
        ['Role'] = { HOUND.DB.RadarType.SEARCH, HOUND.DB.RadarType.TRACK },
        ['Band'] = {
            [true] = { 0.516884, 0.082701 },
            [false] = { 0.136269, 0.013627 }
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['CH_HQ22_SR'] = {
        ['Name'] = "HQ-22 SR",
        ['Assigned'] = { "HQ-22" },
        ['Role'] = { HOUND.DB.RadarType.SEARCH },
        ['Band'] = {
            [true] = { 0.516884, 0.082701 },
            [false] = { 0.516884, 0.082701 }
        },
        ['Primary'] = false,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['CH_HQ22_STR'] = {
        ['Name'] = "HQ-22 STR",
        ['Assigned'] = { "HQ-22" },
        ['Role'] = { HOUND.DB.RadarType.SEARCH, HOUND.DB.RadarType.TRACK },
        ['Band'] = {
            [true] = { 0.516884, 0.082701 },
            [false] = { 0.136269, 0.013627 }
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['CH_PGZ09'] = {
        ['Name'] = "PGZ-09",
        ['Assigned'] = { "AAA" },
        ['Role'] = { HOUND.DB.RadarType.SEARCH, HOUND.DB.RadarType.TRACK },
        ['Band'] = {
            [true] = { 0.136269, 0.013627 },
            [false] = { 0.136269, 0.013627 }
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['CH_PGZ95'] = {
        ['Name'] = "PGZ-95",
        ['Assigned'] = { "AAA" },
        ['Role'] = { HOUND.DB.RadarType.TRACK },
        ['Band'] = {
            [true] = { 0.136269, 0.013627 },
            [false] = { 0.136269, 0.013627 }
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['CH_LD3000'] = {
        ['Name'] = "LD-3000 C-RAM",
        ['Assigned'] = { "AAA" },
        ['Role'] = { HOUND.DB.RadarType.TRACK },
        ['Band'] = {
            [true] = { 0.136269, 0.013627 },
            [false] = { 0.136269, 0.013627 }
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['CH_LD3000_stationary'] = {
        ['Name'] = "LD-3000 C-RAM",
        ['Assigned'] = { "AAA" },
        ['Role'] = { HOUND.DB.RadarType.TRACK },
        ['Band'] = {
            [true] = { 0.136269, 0.013627 },
            [false] = { 0.136269, 0.013627 }
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['CH_MIM104_ANMPQ53_KAT1'] = {
        ['Name'] = "Patriot",
        ['Assigned'] = { "Patriot" },
        ['Role'] = { HOUND.DB.RadarType.SEARCH, HOUND.DB.RadarType.TRACK },
        ['Band'] = {
            [true] = { 0.516884, 0.082701 },
            [false] = { 0.136269, 0.013627 }
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['CH_F124'] = {
        ['Name'] = "F124 Frigate",
        ['Assigned'] = { "Naval" },
        ['Role'] = { HOUND.DB.RadarType.NAVAL },
        ['Band'] = {
            [true] = { 0.516884, 0.082701 },
            [false] = { 0.136269, 0.013627 }
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['CH_BoxerSkyranger'] = {
        ['Name'] = "Boxer",
        ['Assigned'] = { "AAA" },
        ['Role'] = { HOUND.DB.RadarType.SEARCH, HOUND.DB.RadarType.TRACK },
        ['Band'] = {
            [true] = { 0.136269, 0.013627 },
            [false] = { 0.136269, 0.013627 }
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['CH_FlaRakRad'] = {
        ['Name'] = "FlaRakRad",
        ['Assigned'] = { "SHORAD" },
        ['Role'] = { HOUND.DB.RadarType.SEARCH, HOUND.DB.RadarType.TRACK },
        ['Band'] = {
            [true] = { 0.024983, 0.012491 },
            [false] = HOUND.DB.Bands.D
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['CH_SkynexHX'] = {
        ['Name'] = "Skynex",
        ['Assigned'] = { "AAA" },
        ['Role'] = { HOUND.DB.RadarType.TRACK },
        ['Band'] = {
            [true] = { 0.136269, 0.013627 },
            [false] = { 0.136269, 0.013627 }
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['CH_Skyshield_FCU'] = {
        ['Name'] = "Skyshield",
        ['Assigned'] = { "AAA" },
        ['Role'] = { HOUND.DB.RadarType.SEARCH, HOUND.DB.RadarType.TRACK },
        ['Band'] = {
            [true] = { 0.516884, 0.082701 },
            [false] = { 0.136269, 0.013627 }
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['CH_TRML4D'] = {
        ['Name'] = "IRIS-T",
        ['Assigned'] = { "SHORAD" },
        ['Role'] = { HOUND.DB.RadarType.SEARCH, HOUND.DB.RadarType.TRACK },
        ['Band'] = {
            [true] = { 0.516884, 0.082701 },
            [false] = { 0.136269, 0.013627 }
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['ELM2048_MMR'] = {
        ['Name'] = "Elta MMR",
        ['Assigned'] = { "Sling" },
        ['Role'] = { HOUND.DB.RadarType.SEARCH, HOUND.DB.RadarType.TRACK },
        ['Band'] = {
            [true] = HOUND.DB.Bands.F,
            [false] = HOUND.DB.Bands.E
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['ELM2084_MMR_AD_SC'] = {
        ['Name'] = "Elta MMR",
        ['Assigned'] = { "Sling" },
        ['Role'] = { HOUND.DB.RadarType.SEARCH, HOUND.DB.RadarType.TRACK },
        ['Band'] = {
            [true] = HOUND.DB.Bands.F,
            [false] = HOUND.DB.Bands.E
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['ELM2084_MMR_AD_RT'] = {
        ['Name'] = "Elta MMR",
        ['Assigned'] = { "Sling" },
        ['Role'] = { HOUND.DB.RadarType.SEARCH },
        ['Band'] = {
            [true] = HOUND.DB.Bands.F,
            [false] = HOUND.DB.Bands.E
        },
        ['Primary'] = false,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['ELM2084_MMR_WLR'] = {
        ['Name'] = "Elta MMR",
        ['Assigned'] = { "Sling" },
        ['Role'] = { HOUND.DB.RadarType.SEARCH, HOUND.DB.RadarType.TRACK },
        ['Band'] = {
            [true] = HOUND.DB.Bands.F,
            [false] = HOUND.DB.Bands.E
        },
        ['Primary'] = true,
        ['numDistinctFreqs'] = 0
    }
    HOUND.DB.Radars['EWR P-14 Tall King'] = {
        ['Name'] = "Tall King",
        ['Assigned'] = { "EWR" , "SA-5" },
        ['Role'] = { HOUND.DB.RadarType.EWR, HOUND.DB.RadarType.SEARCH },
        ['Band'] = {
            [true] = {1.620500,0.253203},
            [false] = {1.620500,0.253203}
        },
        ['Primary'] = false,
        ['numDistinctFreqs'] = 4
    }
end
do
    local l_mist = HOUND.Mist
    local l_math = math

    function HOUND.DB.getRadarData(typeName)
        if not HOUND.DB.Radars[typeName] then return end
        local data = l_mist.utils.deepCopy(HOUND.DB.Radars[typeName])
        data.isEWR = HOUND.setContainsValue(data.Role,HOUND.DB.RadarType.EWR)
        local randomFactor = nil
        if data.numDistinctFreqs and data.numDistinctFreqs > 0 then
            local spacing = 1/(data.numDistinctFreqs+1)
            randomFactor = HOUND.Clamp(HOUND.Gaussian(spacing * l_math.random(data.numDistinctFreqs), spacing/6),0,1)
        end
        data.Freqency = HOUND.DB.getEmitterFrequencies(data.Band, randomFactor)
        return data
    end

    function HOUND.DB.isValidPlatform(candidate,PayloadAdded)
        if (not HOUND.Utils.Dcs.isUnit(candidate) and not HOUND.Utils.Dcs.isStaticObject(candidate)) or not candidate:isExist()
             then return false
        end

        local isValid = false
        local mainCategory = Object.getCategory(candidate)
        local UnitType = candidate:getTypeName()
        if HOUND.setContains(HOUND.DB.Platform,mainCategory) then
            if HOUND.setContains(HOUND.DB.Platform[mainCategory],UnitType) then
                if HOUND.DB.Platform[mainCategory][UnitType]['require'] then
                    local platformData = HOUND.DB.Platform[mainCategory][UnitType]
                    if HOUND.setContains(platformData['require'],'Payload') then
                        HOUND.Logger.debug(tostring(PayloadAdded).. " | " .. type(PayloadAdded))
                        local required = platformData['require']['Payload']
                        if type(PayloadAdded) == 'string' then
                            HOUND.Logger.debug("HOUND.DB.isValidPlatform: checking payload " .. PayloadAdded .. " for platform " .. UnitType)
                            isValid = HOUND.setContainsValue(required,PayloadAdded)
                            HOUND.Logger.debug("HOUND.DB.isValidPlatform: isValid = " .. tostring(isValid))
                        else
                            isValid = HOUND.Utils.hasPayload(candidate,required)
                        end
                    end
                    if HOUND.setContains(platformData['require'],'TASK') then
                        local required = platformData['require']['TASK']
                        isValid = not HOUND.Utils.hasTask(candidate,required)
                    end
                else
                    isValid = true
                end
            end
        end
        return isValid
    end

    function HOUND.DB.getPlatformData(DcsObject)
        if not HOUND.Utils.Dcs.isUnit(DcsObject) and not HOUND.Utils.Dcs.isStaticObject(DcsObject) then return end

        local platformData={
            pos = l_mist.utils.deepCopy(DcsObject:getPosition().p),
            isStatic = false,
            isAerial = false,
        }

        local mainCategory, PlatformUnitCategory = DcsObject:getCategory()
        local typeName = DcsObject:getTypeName()
        local DbInfo = HOUND.DB.Platform[mainCategory][typeName]

        local errorDist = DbInfo.ins_error or 0
        platformData.posErr = HOUND.Utils.Vector.getRandomVec2(errorDist)
        platformData.posErr.y = 0
        platformData.ApertureSize = (DbInfo.antenna.size * DbInfo.antenna.factor) or 0

        local VerticalOffset = DbInfo.antenna.size
        local objHitBox = DcsObject:getDesc()["box"]
        if objHitBox then
            VerticalOffset = objHitBox["max"]["y"]
        end
        if mainCategory == Object.Category.STATIC then
            platformData.isStatic = true
            platformData.pos.y = platformData.pos.y + VerticalOffset/2
        else
            if PlatformUnitCategory == Unit.Category.HELICOPTER or PlatformUnitCategory == Unit.Category.AIRPLANE then
                platformData.isAerial = true
            end
            if PlatformUnitCategory == Unit.Category.GROUND_UNIT then
                platformData.pos.y = platformData.pos.y + VerticalOffset
            end
        end
        if not platformData.isAerial then
            platformData.pos.y = platformData.pos.y + VerticalOffset
        end
        return platformData
    end

    function HOUND.DB.getDefraction(wavelength,antenna_size)
        if wavelength == nil or antenna_size == nil or antenna_size == 0 then return l_math.rad(30) end
        return wavelength/antenna_size
    end

    function HOUND.DB.getApertureSize(DcsObject)
        if not HOUND.Utils.Dcs.isUnit(DcsObject) and not HOUND.Utils.Dcs.isStaticObject(DcsObject) then return 0 end
        local mainCategory = Object.getCategory(DcsObject)
        local typeName = DcsObject:getTypeName()
        if HOUND.setContains(HOUND.DB.Platform,mainCategory) then
            if HOUND.setContains(HOUND.DB.Platform[mainCategory],typeName) then
                return HOUND.DB.Platform[mainCategory][typeName].antenna.size * HOUND.DB.Platform[mainCategory][typeName].antenna.factor * HOUND.ANTENNA_FACTOR
            end
        end
        return 0
    end

    function HOUND.DB.getEmitterBand(DcsUnit)
        if not HOUND.Utils.Dcs.isUnit(DcsUnit) then return HOUND.DB.Bands.C end
        local typeName = DcsUnit:getTypeName()
        local _,isTracking = DcsUnit:getRadar()
        if HOUND.setContains(HOUND.DB.Radars,typeName) then
            return HOUND.DB.Radars[typeName].Band[HOUND.Utils.Dcs.isUnit(isTracking)]
        end
        return HOUND.DB.Bands.C
    end

    function HOUND.DB.getEmitterFrequencies(bands,factor)
        local freqFactor = factor or l_math.random()
        return {
            [true] = bands[true][1] + bands[true][2] * freqFactor,
            [false] = bands[false][1] + bands[false][2] * freqFactor
        }
    end

    function HOUND.DB.getSensorPrecision(platform,emitterFreq)
        local wavelength = emitterFreq
        if HOUND.Utils.Dcs.isUnit(emitterFreq) then
            local _,track = emitterFreq:getRadar()
            wavelength = HOUND.DB.getEmitterFrequencies(HOUND.DB.getEmitterBand(emitterFreq))[HOUND.Utils.Dcs.isUnit(track)]
        end

        return HOUND.DB.getDefraction(wavelength,HOUND.DB.getApertureSize(platform)) or l_math.rad(20.0) -- precision
    end

    function HOUND.DB.updateHumanDb(coalitionId)
        local coalitions = coalition.side
        if type(coalitionId == "number") and (coalitionId >= 0 and coalitionId <= 2) then
            coalitions = { coalitionId }
        end
        for _,coa in pairs(coalitions) do
            local activeCoaPlayers = HOUND.Utils.Dcs.getPlayers(coa)
            for unitName,player in pairs(activeCoaPlayers) do
                if not HOUND.DB.HumanUnits.byName[coa][unitName] then
                    HOUND.DB.HumanUnits.byName[coa][unitName] = HOUND.Mist.utils.deepCopy(player)
                else
                    for k,v in pairs(player) do
                        HOUND.DB.HumanUnits.byName[coa][unitName][k] = player[k]
                    end
                end
                local gid = player.groupId
                if type(HOUND.DB.HumanUnits.byGid[coa][gid]) ~= "table" then
                    HOUND.DB.HumanUnits.byGid[coa][gid] = {}
                end
                HOUND.DB.HumanUnits.byGid[coa][gid][unitName] = HOUND.DB.HumanUnits.byName[coa][unitName]
            end
        end
    end

    function HOUND.DB.cleanHumanDb(coalitionId)
        local coalitions = coalition.side
        if type(coalitionId == "number") and (coalitionId >= 0 and coalitionId <= 2) then
            coalitions = { coalitionId }
        end
        for _,coa in pairs(coalitions) do
            for unitName,player in pairs(HOUND.DB.HumanUnits.byName[coa]) do
                if HOUND.Utils.absTimeDelta(player.lastSeen) > 300 then
                    local gid = player.groupId
                    HOUND.DB.HumanUnits.byName[coa][unitName] = nil
                    HOUND.DB.HumanUnits.byGid[coa][gid][unitName] = nil
                    if length(HOUND.DB.HumanUnits.byGid[coa][gid]) == 0 then
                        HOUND.DB.HumanUnits.byGid[coa][gid] = nil
                    end
                end
            end
        end
    end

    function HOUND.DB.generateMistDbEntry(DcsUnit)
        if not HOUND.Utils.Dcs.isUnit(DcsUnit) then return {} end
        local grp = DcsUnit:getGroup()
        local unitCallsign = DcsUnit:getCallsign()
        local parsedCallsign = {unitCallsign:match("([%a]+)(%d+)%-(%d+)")}
        if #parsedCallsign ~= 3 then
            parsedCallsign = {unitCallsign:match("([%a]+)(%d)(%d)")}
        end
        local unitData = {
            type = DcsUnit:getTypeName(),
            unitId = DcsUnit:getID(),
            unitName = DcsUnit:getName(),
            lastSeen = timer:getAbsTime(),
            groupId = grp:getID(),
            groupName = grp:getName(),
            callsign = {
                [1] = parsedCallsign[1],
                [2] = tonumber(parsedCallsign[2]),
                [3] = tonumber(parsedCallsign[3]),
                name = unitCallsign
            }
        }
        return unitData
    end
end--- HOUND.Config
do

    HOUND.Config = {
        configMaps = {}
    }

    HOUND.Config.__index = HOUND.Config

    function HOUND.Config.get(HoundInstanceId)
        HoundInstanceId = HoundInstanceId or HOUND.Length(HOUND.Config.configMaps)+1

        if HOUND.Config.configMaps[HoundInstanceId] then
            return HOUND.Config.configMaps[HoundInstanceId]
        end

        local instance = {}
        instance.intervals = {
            scan = 10,
            process = 30,
            menus = 60,
            markers = 120,

        }
        instance.preferences = {
            useMarkers = true,
            markerType = HOUND.MARKER.CIRCLE,
            markSites = true,
            hardcore = false,
            detectDeadRadars = true,
            NatoBrevity = false,
            platformPosErr = false,
            useNatoCallsigns = false,
            AtisUpdateInterval = 300,
            AlertOnLaunch = false,
            AlertOnLaunchCooldown = 30

        }
        instance.coalitionId = nil
        instance.id = HoundInstanceId
        instance.callsigns = {}
        instance.callsignOverride = {}
        instance.radioMenu = {
            root = nil,
            parent = nil
        }
        instance.onScreenDebug = false

        instance.getId = function (self)
            return self.id
        end

        instance.getCoalition = function(self)
            return self.coalitionId
        end

        instance.setCoalition = function(self,coalitionId)
            if self.coalitionId ~= nil then
                env.info("[Hound] - coalition already set for Instance Id " .. self.id)
                return false
            end
            if HOUND.setContainsValue(coalition.side,coalitionId) then
                self.coalitionId = coalitionId
                return true
            end
            return false
        end

        instance.setInterval = function (self,intervalName,setVal)
            if HOUND.setContains(self.intervals,intervalName) and type(setVal) == "number" then
                self.intervals[intervalName] = setVal
                return true
            end
            return false
        end

        instance.getMarkerType = function (self)
            return self.preferences.markerType
        end

        instance.setMarkerType = function (self,markerType)
            if HOUND.setContainsValue(HOUND.MARKER,markerType) then
                self.preferences.markerType = markerType
                return true
            end
            return false
        end

        instance.getUseMarkers = function (self)
            return self.preferences.useMarkers
        end

        instance.setUseMarkers = function(self,value)
            if type(value) == "boolean" then
                self.preferences.useMarkers = value
                return true
            end
            return false
        end
        instance.getMarkSites = function (self)
            return self.preferences.markSites
        end
        instance.setMarkSites = function(self,value)
            if type(value) == "boolean" then
                self.preferences.markSites = value
                return true
            end
            return false
        end

        instance.getBDA = function(self)
            return self.preferences.detectDeadRadars
        end

        instance.setBDA = function(self,value)
            if type(value) == "boolean" then
                self.preferences.detectDeadRadars = value
                return true
            end
            return false
        end

        instance.getNATO = function(self)
            return self.preferences.NatoBrevity
        end

        instance.setNATO = function(self,value)
            if type(value) == "boolean" then
                self.preferences.NatoBrevity = value
                return true
            end
            return false
        end

        instance.getUseNATOCallsigns = function(self)
            return self.preferences.useNatoCallsigns
        end

        instance.setUseNATOCallsigns = function(self,value)
            if type(value) == "boolean" then
                self.preferences.useNatoCallsigns = value
                return true
            end
            return false
        end

        instance.getAtisUpdateInterval = function(self)
            return self.preferences.AtisUpdateInterval
        end

        instance.setAtisUpdateInterval = function(self,value)
            if type(value) == "number" then
                self.preferences.AtisUpdateInterval = value
                return true
            end
            return false
        end

        instance.getPosErr = function(self)
            return self.preferences.platformPosErr
        end

        instance.setPosErr = function(self,value)
            if type(value) == "boolean" then
                self.preferences.platformPosErr = value
                return true
            end
            return false
        end

        instance.getHardcore = function(self)
            return self.preferences.hardcore
        end

        instance.setHardcore = function(self,value)
            if type(value) == "boolean" then
                self.preferences.hardcore = value
                return true
            end
            return false
        end

        instance.getOnScreenDebug = function(self)
            return self.onScreenDebug
        end

        instance.setOnScreenDebug = function(self,value)
            if type(value) == "boolean" then
                self.onScreenDebug = value
                return true
            end
            return false
        end

        instance.getCallsignOverride = function(self)
            return self.callsignOverride
        end

        instance.setCallsignOverride = function(self,value)
            if type(value) == "table" then
                self.callsignOverride = value
                return true
            end
            return false
        end

        instance.setAlertOnLaunch = function(self,value)
            if type(value) == "boolean" then
                self.preferences.AlertOnLaunch = value
                return true
            end
            return false
        end

        instance.getAlertOnLaunch = function(self)
            return self.preferences.AlertOnLaunch
        end

        instance.getAlertOnLaunchCooldown = function(self)
            return self.preferences.AlertOnLaunchCooldown
        end

        instance.setAlertOnLaunchCooldown = function(self,value)
            if type(value) == "number" then
                self.preferences.AlertOnLaunchCooldown = value
                return true
            end
            return false
        end

        instance.getRadioMenu = function (self)
            if not self.radioMenu.root then
                self.radioMenu.root = missionCommands.addSubMenuForCoalition(
                    self:getCoalition(), 'ELINT',self:getRadioMenuParent())
            end
            return self.radioMenu.root
        end

        instance.removeRadioMenu = function (self)
            if self.radioMenu.root ~= nil then
                missionCommands.removeItemForCoalition(self:getCoalition(),self.radioMenu.root)
                self.radioMenu.root = nil
                return true
            end
            return false
        end

        instance.getRadioMenuParent = function(self)
            return self.radioMenu.parent
        end

        instance.setRadioMenuParent = function (self,parent)
            if type(parent) == "table" or (parent == nil and self.radioMenu.parent) then
                self:removeRadioMenu()
                self.radioMenu.parent = parent
                return true
            end
            return false
        end

        HOUND.Config.configMaps[HoundInstanceId] = instance

        return HOUND.Config.configMaps[HoundInstanceId]
    end
end
do
    local l_mist = HOUND.Mist
    local l_math = math
    local l_grpc = GRPC
    local PI_2 = 2*l_math.pi

    HOUND.Utils = {
        Mapping = {},
        Dcs     = {},
        Geo     = {},
        Marker  = {},
        Text    = {},
        Elint   = {},
        Vector  = {},
        Zone    = {},
        Sort    = {},
        Filter  = {},
        ReportId = nil,
        _HoundId = 0
    }
    HOUND.Utils.__index = HOUND.Utils

    function HOUND.Utils.getHoundId()
        HOUND.Utils._HoundId = HOUND.Utils._HoundId + 1
        return HOUND.Utils._HoundId
    end

    function HOUND.Utils.getMarkId()
        return HOUND.Utils.Marker.getId()
    end

    function HOUND.Utils.setInitialMarkId(startId)
        return HOUND.Utils.Marker.setInitialId(startId)
    end

    function HOUND.Utils.absTimeDelta(t0, t1)
        if t1 == nil then t1 = timer.getAbsTime() end
        return t1 - t0
    end

    function HOUND.Utils.angleDeltaRad(rad1,rad2)
        if not rad1 or not rad2 then return end
        return l_math.pi - l_math.abs(l_math.pi - l_math.abs(rad1-rad2) % PI_2)
    end

    function HOUND.Utils.normalizeAngle(rad)
        return rad - (PI_2) * l_math.floor((rad + l_math.pi) / (PI_2))
    end

    function HOUND.Utils.AzimuthAverage(azimuths)
        if not azimuths or HOUND.Length(azimuths) == 0 then return nil end

        local sumSin = 0
        local sumCos = 0
        for i=1, HOUND.Length(azimuths) do
            sumSin = sumSin + l_math.sin(azimuths[i])
            sumCos = sumCos + l_math.cos(azimuths[i])
        end
        return (l_math.atan2(sumSin,sumCos) + PI_2) % PI_2
    end

    function HOUND.Utils.getMagVar(DCSpoint)
        if not HOUND.Utils.Dcs.isPoint(DCSpoint) then return 0 end
        return l_mist.getNorthCorrection(DCSpoint)

    end
    function HOUND.Utils.PointClusterTilt(points,MagNorth,refPos)
        if not points or type(points) ~= "table" then return end
        if not refPos then
            refPos = l_mist.getAvgPoint(points)
        end
        local magVar = 0
        if MagNorth then
            magVar = HOUND.Utils.getMagVar(refPos)
        end
        local biasVector = nil
        for _,point in pairs(points) do
            local V = {
                y = 0
            }
            V.x = point.x - refPos.x
            V.z = point.z - refPos.z
            if V.x < 0 then
                V.x = -V.x
                V.z = -V.z
            end
            if biasVector == nil then biasVector = V else biasVector = l_mist.vec.add(biasVector,V) end
        end
        return (l_math.atan2(biasVector.z,biasVector.x) + magVar) % PI_2
    end

    function HOUND.Utils.RandomAngle()
        return l_math.random() * 2 * l_math.pi
    end

    function HOUND.Utils.getRoundedElevationFt(elev,resolution)
        if not resolution then
            resolution = 50
        end
        return HOUND.Utils.roundToNearest(l_mist.utils.metersToFeet(elev),resolution)
    end

    function HOUND.Utils.roundToNearest(input,nearest)
        return l_mist.utils.round(input/nearest) * nearest
    end

    function HOUND.Utils.getNormalAngularError(maxError)
        local stddev = maxError / 2
        local Magnitude = l_math.sqrt(-2 * l_math.log(l_math.random())) * stddev
        local Theta = 2* math.pi * l_math.random()

        local epsilon = {
            az = Magnitude * l_math.cos(Theta),
            el = Magnitude * l_math.sin(Theta)
        }
        return epsilon
    end

    function HOUND.Utils.getControllerResponse()
        local response = {
            " ",
            "Good Luck!",
            "Happy Hunting!",
            "Please send my regards.",
            "Come back with E T A, T O T, and B D A.",
            " "
        }
        return response[l_math.max(1,l_math.min(l_math.ceil(timer.getAbsTime() % HOUND.Length(response)),HOUND.Length(response)))]
    end

    function HOUND.Utils.getCoalitionString(coalitionID)
        local coalitionStr = "RED"
        if coalitionID == coalition.side.BLUE then
            coalitionStr = "BLUE"
        elseif coalitionID == coalition.side.NEUTRAL then
            coalitionStr = "NEUTRAL"
        end
        return coalitionStr
    end

    function HOUND.Utils.getHemispheres(lat,lon,fullText)
        local hemi = {
            NS = "North",
            EW = "East"
        }
        if lat < 0 then hemi.NS = "South" end
        if lon < 0 then hemi.EW = "West" end
        if fullText == nil or fullText == false then
            hemi.NS = string.sub(hemi.NS, 1, 1)
            hemi.EW = string.sub(hemi.EW, 1, 1)
        end
        return hemi
    end

    function HOUND.Utils.getReportId(ReportId)
        local returnId
        if ReportId ~= nil then
            returnId =  string.byte(ReportId)
        else
            returnId = HOUND.Utils.ReportId
        end
        if returnId == nil or returnId == string.byte('Z') then
            returnId = string.byte('A')
        else
            returnId = returnId + 1
        end
        if not ReportId then
            HOUND.Utils.ReportId = returnId
        end

        return HOUND.DB.PHONETICS[string.char(returnId)],string.char(returnId)
    end

    function HOUND.Utils.DecToDMS(cood)
        local deg = l_math.floor(cood)
        if cood < 0 then
            deg = l_math.ceil(cood)
        end
        local minutes = l_math.floor(l_math.abs(cood - deg) * 60)
        local sec = l_math.floor((l_math.abs(cood-deg) * 3600) % 60)
        local dec = l_math.abs(cood-deg) * 60

        return {
            d = deg,
            m = minutes,
            s = sec,
            mDec = l_mist.utils.round(dec ,3),
            sDec = l_mist.utils.round((l_mist.utils.round(dec,3)*1000)-(minutes*1000))
        }
    end

    function HOUND.Utils.getBR(src,dst)
        if not src or not dst then return end
        local BR = {}
        local dir = l_mist.utils.getDir(l_mist.vec.sub(dst,src),src) -- pass src to get magvar included
        BR.brg = l_mist.utils.round(l_mist.utils.toDegree( dir ))
        BR.brStr = string.format("%03d",BR.brg)
        BR.rng = l_mist.utils.round(l_mist.utils.metersToNM(l_mist.utils.get2DDist(dst,src)))
        return BR
    end

    function HOUND.Utils.getFormationCallsign(player,override,flightMember)
        local callsign = ""
        local DcsUnit = Unit.getByName(player.unitName)
        if type(player) ~= "table" then return callsign end
        if type(flightMember) == "table" and override == nil then
            override,flightMember = flightMember,override
        end
        local formationCallsign = string.gsub(player.callsign.name,"[%d%s]","")

        callsign =  formationCallsign .. " " .. player.callsign[2]
        if flightMember then
            callsign = callsign .. " " .. player.callsign[3]
        end

        if type(override) == "table" then
            if HOUND.setContains(override,formationCallsign) then
                local override = override[formationCallsign]
                if override == "*" then
                    override = DcsUnit:getGroup():getName() or formationCallsign
                end
                callsign = callsign:gsub(formationCallsign,override)
                return string.upper(callsign:match( "^%s*(.-)%s*$" ))
            end
        end

        if not DcsUnit then return string.upper(callsign:match( "^%s*(.-)%s*$" )) end

        local playerName = DcsUnit:getPlayerName()
        playerName = playerName:match("%a+%s%d+[?%p%s*]%d*")
        if playerName then
            callsign = playerName
            local base = string.match(callsign,"%a+")
            local num = tonumber(string.match(callsign,"%d+"))
            local memberNum = string.gsub(callsign,"%a+%s%d+[%p%s*]","")
            if memberNum:len() > 0 then
                memberNum = tonumber(memberNum:match("%d+"))
            else
                memberNum = nil
            end

            callsign = base
            if type(num) == "number" and type(memberNum) == "number" then
                callsign = callsign .. " " .. num
            end

            if flightMember then
                if type(memberNum) == "number" then
                    callsign = callsign .. " " .. memberNum
                end
                if type(num) == "number" and type(memberNum) == "nil" then
                    callsign = callsign .. " " .. num
                end
            end
            return string.upper(callsign:match( "^%s*(.-)%s*$" ))
        end
        return string.upper(callsign:match( "^%s*(.-)%s*$" ))
    end

    function HOUND.Utils.getHoundCallsign(namePool)
        local SelectedPool = HOUND.DB.CALLSIGNS[namePool] or HOUND.DB.CALLSIGNS.GENERIC
        return SelectedPool[l_math.random(1, HOUND.Length(SelectedPool))]
    end

    function HOUND.Utils.useDMM(DcsUnit)
        if not DcsUnit then return false end
        local typeName = nil
        if type(DcsUnit) == "string" then
            typeName = DcsUnit
        end
        if HOUND.Utils.Dcs.isUnit(DcsUnit) then
            typeName = DcsUnit:getTypeName()
        end
        return HOUND.setContains(HOUND.DB.useDMM,typeName)
    end

    function HOUND.Utils.useMGRS(DcsUnit)
        if not DcsUnit then return false end
        local typeName = nil
        if type(DcsUnit) == "string" then
            typeName = DcsUnit
        end
        if HOUND.Utils.Dcs.isUnit(DcsUnit) then
            typeName = DcsUnit:getTypeName()
        end
        return HOUND.setContains(HOUND.DB.useMGRS,typeName)
    end
    function HOUND.Utils.hasPayload(DcsUnit,payloadName)
        return true
    end

    function HOUND.Utils.hasTask(DcsUnit,taskName)
        return true
    end

    HOUND.Utils.Mapping.CURVES = {
        RETAIL = 0,
        WINDOWS = 1,
        HERRA9 = 2,
        HERRA45 = 3,
        EXPONENTIAL = 4,
        MIXED = 5,
        POWER = 6
    }

    function HOUND.Utils.Mapping.clamp(input, out_min, out_max)
        return l_math.max(out_min,l_math.min(input,out_max))
    end

    function HOUND.Utils.Mapping.linear(input, in_min, in_max, out_min, out_max,clamp)
        local mapValue = (input - in_min) * (out_max - out_min) / (in_max - in_min) + out_min
        if clamp then
            if out_min < out_max then
                return l_math.max(out_min,l_math.min(out_max,mapValue))
            else
                return l_math.max(out_max,l_math.min(out_min,mapValue))
            end
        end
        return mapValue
    end

    function HOUND.Utils.Mapping.nonLinear(value,in_min,in_max,out_min,out_max,sensitivity,curve_type)

        if type(sensitivity) ~= "number" then
            sensitivity = 9
        end
        sensitivity=l_math.min(0,l_math.max(9,sensitivity))
        local relativePos = HOUND.Utils.Mapping.linear(value,in_min,in_max,0,1)
        local mappedIn = relativePos*(sensitivity/9)+(relativePos^5)*(9-sensitivity)/9
        if type(curve_type) == "number" then
            if curve_type == 1 then
                mappedIn = relativePos^(3-(sensitivity/4.5))
            elseif curve_type == 2 then
                mappedIn = relativePos^(sensitivity/9)*((1-l_math.cos(relativePos*l_math.pi))/2)^((9-sensitivity)/9)
            elseif curve_type == 3 then
                mappedIn = relativePos^(sensitivity/9)*((1-l_math.cos(relativePos*l_math.pi))/2)^((9-sensitivity)/4.5)
            elseif curve_type == 4 then
                mappedIn = (l_math.exp((10-sensitivity)*relativePos)-1)/(l_math.exp(10-sensitivity)-1)
            elseif curve_type == 5 then
                mappedIn = relativePos^(1+((5-sensitivity)/9))
            elseif curve_type == 6 then
                mappedIn = relativePos*relativePos^((9-sensitivity)/9)
            end
        end

        if type(out_min) == "number" and type(out_max) == "number" then
            return HOUND.Utils.Mapping.linear(mappedIn,0,1,out_min,out_max)
        end
        return mappedIn
    end

    function HOUND.Utils.Dcs.isPoint(point)
        if type(point) ~= "table" then return false end
        return (type(point.x) == "number") and (type(point.z) == "number")
    end

    function HOUND.Utils.Dcs.isUnit(obj)
        if type(obj) ~= "table" then return false end
        return getmetatable(obj) == Unit
    end

    function HOUND.Utils.Dcs.isGroup(obj)
        if type(obj) ~= "table" then return false end
        return getmetatable(obj) == Group
    end

    function HOUND.Utils.Dcs.isStaticObject(obj)
        if type(obj) ~= "table" then return false end
        return getmetatable(obj) == StaticObject
    end

    function HOUND.Utils.Dcs.isHuman(obj)
        if not HOUND.Utils.Dcs.isUnit(obj) then return false end
        return obj:getPlayerName() ~= nil
    end

    function HOUND.Utils.Dcs.getPlayers(coalitionId)
        if type(coalitionId) ~= "number" or (coalitionId > 2 or coalitionId < 0) then return {} end
        local players = coalition.getPlayers(coalitionId)
        local humanUnits = {}
        for i = 1, #players do
            local playerUnit = players[i]
            local _,catEx = playerUnit:getCategory()
            if HOUND.setContainsValue({Unit.Category.AIRPLANE,Unit.Category.HELICOPTER},catEx) then
                local unit_data = HOUND.DB.generateMistDbEntry(playerUnit)
                humanUnits[unit_data.unitName] = unit_data
            end
        end
        return humanUnits
    end

    function HOUND.Utils.Dcs.getPlayersInGroup(DcsGroup)
        if type(DcsGroup) == "string" then
            DcsGroup = Group.getByName(DcsGroup)
        end
        if not HOUND.Utils.Dcs.isGroup(DcsGroup) then return {} end
        local coa = DcsGroup:getCoalition()
        local gid = DcsGroup:getID()
        if type(HOUND.DB.HumanUnits.byGid[coa][gid]) ~= "table" then return {} end
        local humanUnits = {}
        for unitName,unitData in pairs(HOUND.DB.HumanUnits.byGid[coa][gid]) do
            humanUnits[unitName] = unitData
        end
        return humanUnits
    end

    function HOUND.Utils.Dcs.isRadarTracking(DcsUnit)
        if not HOUND.Utils.Dcs.isUnit(DcsUnit) then return false end
        local _,isTracking = DcsUnit:getRadar()
        return HOUND.Utils.Dcs.isUnit(isTracking)
    end

    function HOUND.Utils.Dcs.getSamMaxRange(DcsUnit)
        local maxRng = 0
        if DcsUnit ~= nil then
            local units = DcsUnit:getGroup():getUnits()
            for _, unit in ipairs(units) do
                local weapons = unit:getAmmo()
                if weapons ~= nil then
                    for _, ammo in ipairs(weapons) do
                        if ammo.desc.category == Weapon.Category.MISSILE and ammo.desc.missileCategory == Weapon.MissileCategory.SAM then
                            maxRng = l_math.max(l_math.max(ammo.desc.rangeMaxAltMax,ammo.desc.rangeMaxAltMin),maxRng)
                        end
                    end
                end
            end
        end
        return maxRng
    end

    function HOUND.Utils.Dcs.getRadarDetectionRange(DcsUnit)
        local detectionRange = 0
        local unit_sensors = DcsUnit:getSensors()
        if not unit_sensors then return detectionRange end
        if not HOUND.setContains(unit_sensors,Unit.SensorType.RADAR) then return detectionRange end
        for _,radar in pairs(unit_sensors[Unit.SensorType.RADAR]) do
            if HOUND.setContains(radar,"detectionDistanceAir") then
                for _,aspects in pairs(radar["detectionDistanceAir"]) do
                    for _,range in pairs(aspects) do
                        detectionRange = l_math.max(detectionRange,range)
                    end
                end
            end
        end
        return detectionRange
    end

    function HOUND.Utils.Dcs.getRadarUnitsInGroup(DcsGroup)
        local radarUnits = {}
        if HOUND.Utils.Dcs.isGroup(DcsGroup) and DcsGroup:isExist() and DcsGroup:getSize() > 0 then
            for _,unit in ipairs(DcsGroup:getUnits()) do
                if unit:hasSensors(Unit.SensorType.RADAR) and HOUND.setContains(HOUND.DB.Radars,unit:getTypeName()) then
                    table.insert(radarUnits,unit)
                end
            end
        end
        return radarUnits
    end

    function HOUND.Utils.Dcs.getGroupNames(prefix)
        local groups = {}
        if type(prefix) ~= "string" then
            prefix = nil
        end
        for _,coalitionName in pairs(coalition.side) do
            for _,group in pairs(coalition.getGroups(coalitionName)) do
                local groupName = group:getName()
                if prefix == nil or (prefix ~= "" and string.find(groupName, prefix, 1, true) == 1) then
                    groups[groupName] = group:getID()
                end
            end
        end
        return groups
    end

    function HOUND.Utils.Dcs.getUnitNames(prefix)
        local units = {}
        if type(prefix) ~= "string" then
            prefix = nil
        end
        for _,coalitionName in pairs(coalition.side) do
            for _,group in pairs(coalition.getGroups(coalitionName)) do
                for _,unit in pairs(group:getUnits()) do
                    local unitName = unit:getName()
                    if prefix == nil or (prefix ~= "" and string.find(unitName, prefix, 1, true) == 1) then
                        units[unitName] = HOUND.DB.generateMistDbEntry(unit)
                    end
                end
            end
        end
        return units
    end

    function HOUND.Utils.Dcs.getStaticObjectNames(prefix)
        local staticObjs = {}
        if type(prefix) ~= "string" then
            prefix = nil
        end
        for _,coalitionName in pairs(coalition.side) do
            for _,staticObj in pairs(coalition.getStaticObjects(coalitionName)) do
                local name = staticObj:getName()
                if prefix == nil or (prefix ~= "" and string.find(name, prefix, 1, true) == 1) then
                    staticObjs[name] = name
                end

            end
        end
        return staticObjs
    end

    function HOUND.Utils.Dcs.getGroupPoints(groupIdent)
        local gpId = groupIdent
        if type(groupIdent) == 'string' and not tonumber(groupIdent) then
            for grpName,grpId in pairs(HOUND.Utils.Dcs.getGroupNames(groupIdent)) do
                if grpName == groupIdent then
                    gpId = grpId
                end
            end
            if gpId == groupIdent then
                log:error("Group not found: $1", groupIdent)
            end
        end

        for coa_name, coa_data in pairs(env.mission.coalition) do
            if  type(coa_data) == 'table' then
                if coa_data.country then --there is a country table
                    for cntry_id, cntry_data in pairs(coa_data.country) do
                        for obj_cat_name, obj_cat_data in pairs(cntry_data) do
                            if obj_cat_name == "helicopter" or obj_cat_name == "ship" or obj_cat_name == "plane" or obj_cat_name == "vehicle" then	-- only these types have points
                                if ((type(obj_cat_data) == 'table') and obj_cat_data.group and (type(obj_cat_data.group) == 'table') and (#obj_cat_data.group > 0)) then	--there's a group!
                                    for group_num, group_data in pairs(obj_cat_data.group) do
                                        if group_data and group_data.groupId == gpId then -- this is the group we are looking for
                                            if group_data.route and group_data.route.points and #group_data.route.points > 0 then
                                                local points = {}
                                                for point_num, point in pairs(group_data.route.points) do
                                                    if not point.point then
                                                        points[point_num] = { x = point.x, y = point.y }
                                                    else
                                                        points[point_num] = point.point	--it's possible that the ME could move to the point = Vec2 notation.
                                                    end
                                                end
                                                return points
                                            end
                                            return
                                        end	--if group_data and group_data.name and group_data.name == 'groupname'
                                    end --for group_num, group_data in pairs(obj_cat_data.group) do
                                end --if ((type(obj_cat_data) == 'table') and obj_cat_data.group and (type(obj_cat_data.group) == 'table') and (#obj_cat_data.group > 0)) then
                            end --if obj_cat_name == "helicopter" or obj_cat_name == "ship" or obj_cat_name == "plane" or obj_cat_name == "vehicle" or obj_cat_name == "static" then
                        end --for obj_cat_name, obj_cat_data in pairs(cntry_data) do
                    end --for cntry_id, cntry_data in pairs(coa_data.country) do
                end --if coa_data.country then --there is a country table
            end --if coa_name == 'red' or coa_name == 'blue' and type(coa_data) == 'table' then
        end --for coa_name, coa_data in pairs(mission.coalition) do
    end

    function HOUND.Utils.Geo.checkLOS(pos0,pos1)
        if not HOUND.Utils.Dcs.isPoint(pos0) or not HOUND.Utils.Dcs.isPoint(pos1) then return false end
        local dist = l_mist.utils.get2DDist(pos0,pos1)
        local radarHorizon = HOUND.Utils.Geo.EarthLOS(pos0.y,pos1.y)
        return (dist <= radarHorizon*1.025 and land.isVisible(pos0,pos1))
    end

    function HOUND.Utils.Geo.EarthLOS(h0,h1)
        if not h0 then return 0 end
        local Re = 6367444 -- Radius of earth in M (avarage radius of WGS84)
        local d0 = l_math.sqrt(h0^2+2*Re*h0)
        local d1 = 0
        if h1 then d1 = l_math.sqrt(h1^2+2*Re*h1) end
        return d0+d1
    end

    function HOUND.Utils.Geo.getProjectedIP(p0,az,el)
        if not HOUND.Utils.Dcs.isPoint(p0) or type(az) ~= "number" or type(el) ~= "number" then return end
        local maxSlant = HOUND.Utils.Geo.EarthLOS(p0.y)*1.1

        local unitVector = HOUND.Utils.Vector.getUnitVector(az,el)
        return land.getIP(p0, unitVector , maxSlant )
    end

    function HOUND.Utils.Geo.setPointHeight(point,offset)
        if HOUND.Utils.Dcs.isPoint(point) and type(point.y) ~= "number" then
            offset = offset or 0
            point.y = land.getHeight({x=point.x,y=point.z}) + offset
        end
        return point
    end

    function HOUND.Utils.Geo.setHeight(point,offset)
        if type(point) == "table" then
            offset = offset or 0
            if HOUND.Utils.Dcs.isPoint(point) then
                return HOUND.Utils.Geo.setPointHeight(point,offset)
            end
            for _,pt in pairs(point) do
                pt = HOUND.Utils.Geo.setPointHeight(pt,offset)
            end
        end
        return point
    end

    function HOUND.Utils.Geo.get2DDistance(src, dst)
        if HOUND.Utils.Dcs.isPoint(src) and HOUND.Utils.Dcs.isPoint(dst) then
            return l_mist.utils.get2DDist(src,dst)
        end

    end

    function HOUND.Utils.Geo.get3DDistance(src, dst)
        if HOUND.Utils.Dcs.isPoint(src) and HOUND.Utils.Dcs.isPoint(dst) then
            return l_mist.utils.get3DDist(src,dst)
        end

    end

    HOUND.Utils.Marker._MarkId = 4999
    HOUND.Utils.Marker.Type = {
        NONE = 0,
        POINT = 1,
        TEXT =  2,
        CIRCLE = 3,
        FREEFORM = 4
    }

    function HOUND.Utils.Marker.getId()
            HOUND.Utils.Marker._MarkId = HOUND.Utils.Marker._MarkId + 1
        return HOUND.Utils.Marker._MarkId
    end

    function HOUND.Utils.Marker.setInitialId(startId)
        if type(startId) ~= "number" then
            HOUND.Logger.error("Failed to set Initial marker Id. Value provided was not a number")
            return false
        end
        if HOUND.Utils.Marker._MarkID ~= 0 then
            HOUND.Logger.error("Initial MarkId not updated because markers have already been drawn")
            return false
        end
        HOUND.Utils.Marker._MarkId = startId
        return true
    end

    function HOUND.Utils.Marker.create(args)
        local instance = {}
        instance.id = -1
        instance.type = HOUND.Utils.Marker.Type.NONE

        instance.setPos = function(self,pos)
            if self.type == HOUND.Utils.Marker.Type.FREEFORM then return end
            if HOUND.Utils.Dcs.isPoint(pos) then
                trigger.action.setMarkupPositionStart(self.id,pos)
            end
        end

        instance.setText = function(self,text)
            if type(text) == "string" and self.id > 0 then
                if self.type == HOUND.Utils.Marker.Type.TEXT then
                    text = HOUND.MARKER_TEXT_POINTER .. text
                end
                trigger.action.setMarkupText(self.id,text)
            end
        end

        instance.setRadius = function(self,radius)
            if type(radius) == "number" and self.type == HOUND.Utils.Marker.Type.CIRCLE and self.id > 0 then
                trigger.action.setMarkupRadius(self.id,radius)
            end
        end

        instance.setFillColor = function(self,color)
            if self.id > 0 and self.type ~= HOUND.Utils.Marker.Type.FREEFORM and type(color) == "table" then
                trigger.action.setMarkupColorFill(self.id,color)
            end
        end

        instance.setLineColor = function(self,color)
            if self.id > 0 and self.type ~= HOUND.Utils.Marker.Type.FREEFORM and type(color) == "table" then
                trigger.action.setMarkupColor(self.id,color)
            end
        end

        instance.setLineType = function(self,lineType)
            if self.id > 0 and type(lineType) == "number" and self.type ~= HOUND.Utils.Marker.Type.FREEFORM then
                trigger.action.setMarkupTypeLine(self.id,lineType)
            end
        end

        instance.isDrawn = function(self)
            return (self.id > 0)
        end

        instance.remove = function(self)
            if self.id > 0 then
                local GC = (self.id % 5 == 0)
                trigger.action.removeMark(self.id)
                self.id = -1
                self.type = HOUND.Utils.Marker.Type.NONE
                if GC then
                    collectgarbage("collect")
                end
            end
        end

        instance._new = function(self,args)
            if type(args) ~= "table" then return false end
            local coalition = args.coalition
            local pos = args.pos
            local text = args.text
            local lineColor = args.lineColor or {0,0,0,0.75}
            local fillColor = args.fillColor or {0,0,0,0}
            local lineType = args.lineType or 2
            local fontSize = args.fontSize or 16
            if self.id < 1 then
                self.id = HOUND.Utils.Marker.getId()
            end
            if HOUND.Utils.Dcs.isPoint(pos) then
                if args.useLegacyMarker then
                    self.type = HOUND.Utils.Marker.Type.POINT
                    trigger.action.markToCoalition(self.id, text, pos, coalition,true)
                    return true
                end
                self.type = HOUND.Utils.Marker.Type.TEXT
                trigger.action.textToAll(coalition,self.id, pos,lineColor,fillColor,fontSize,true,HOUND.MARKER_TEXT_POINTER .. text)
                return true
            end

            if HOUND.Length(pos) == 2 and HOUND.Utils.Dcs.isPoint(pos.p) and type(pos.r) == "number" then
                self.type = HOUND.Utils.Marker.Type.CIRCLE
                trigger.action.circleToAll(coalition,self.id, pos.p,pos.r,lineColor,fillColor,lineType,true)
                return true
            end

            if HOUND.Length(pos) == 4 then
                self.type = HOUND.Utils.Marker.Type.FREEFORM
                trigger.action.markupToAll(6,coalition,self.id,
                    pos[1], pos[2], pos[3], pos[4],
                    lineColor,fillColor,lineType,true)
            end

            if HOUND.Length(pos) == 8 then
                self.type = HOUND.Utils.Marker.Type.FREEFORM
                trigger.action.markupToAll(7,coalition,self.id,
                    pos[1], pos[2], pos[3], pos[4],
                    pos[5], pos[6], pos[7], pos[8],
                    lineColor,fillColor,lineType,true)
            end

            if HOUND.Length(pos) == 16 then
                self.type = HOUND.Utils.Marker.Type.FREEFORM
                trigger.action.markupToAll(7,coalition,self.id,
                    pos[1], pos[2], pos[3], pos[4],
                    pos[5], pos[6], pos[7], pos[8],
                    pos[9], pos[10], pos[11], pos[12],
                    pos[13], pos[14], pos[15], pos[16],
                    lineColor,fillColor,lineType,true)
            end
        end

        instance._replace = function(self,args)
            self:remove()
            return self:_new(args)
        end

        instance.update = function(self,args)
            if type(args.coalition) ~= "number" then return false end
            if self.id < 1 then
                return self:_new(args)
            end

            if (self.type ==  HOUND.Utils.Marker.Type.POINT or self.type == HOUND.Utils.Marker.Type.FREEFORM) then
                return self:_replace(args)
            end
            if args.pos then
                local pos = args.pos
                if HOUND.Utils.Dcs.isPoint(pos) then
                    self:setPos(pos)
                end
                if HOUND.Length(pos) == 2 and type(pos.r) == "number" and HOUND.Utils.Dcs.isPoint(pos.p) then
                    self:setPos(pos.p)
                    self:setRadius(pos.r)
                end
                if type(pos) == "table" and HOUND.Length(pos) > 2 and HOUND.Utils.Dcs.isPoint(pos[1]) then
                    return self:_replace(args)
                end
            end
            if args.text and type(args.text) == "string" then
                self:setText(args.text)
            end
            if type(args.fillColor) == "table" then
                self:setFillColor(args.fillColor)
            end
            if type(args.lineColor) == "table" then
                self:setLineColor(args.lineColor)
            end
            if type(args.lineType) == "number" then
                self:setLineType(args.lineType)
            end
        end
        if type(args) == "table" then
            instance.update(instance,args)
        end
        return instance
    end

    function HOUND.Utils.Text.getLL(lat,lon,minDec)
        local hemi = HOUND.Utils.getHemispheres(lat,lon)
        lat = HOUND.Utils.DecToDMS(lat)
        lon = HOUND.Utils.DecToDMS(lon)
        if minDec == true then
            return hemi.NS .. string.format("%02d",l_math.abs(lat.d)) .. "°" .. string.format("%.3f",lat.mDec) .. "'" ..  " " ..  hemi.EW  .. string.format("%03d",l_math.abs(lon.d)) .. "°" .. string.format("%.3f",lon.mDec) .. "'"
        end
        return hemi.NS .. string.format("%02d",l_math.abs(lat.d)) .. "°" .. string.format("%02d",lat.m) .. "'".. string.format("%02d",l_math.floor(lat.s)).."\"" ..  " " ..  hemi.EW  .. string.format("%03d",l_math.abs(lon.d)) .. "°" .. string.format("%02d",lon.m) .. "'".. string.format("%02d",l_math.floor(lon.s)) .."\""
    end

    function HOUND.Utils.Text.getTime(timestamp)
        if timestamp == nil then timestamp = timer.getAbsTime() end
        local DHMS = l_mist.time.getDHMS(timestamp)
        return string.format("%02d",DHMS.h)  .. string.format("%02d",DHMS.m)
    end

    function HOUND.Utils.Elint.generateAngularError(maxError)
        local vec2 = HOUND.Utils.Vector.getRandomVec2(maxError)
        local epsilon = {
            az = vec2.x,
            el = vec2.z
        }
        return epsilon
    end

    function HOUND.Utils.Elint.getAzimuth(src, dst, sensorPrecision)
        if not HOUND.Utils.Dcs.isPoint(src) or not HOUND.Utils.Dcs.isPoint(dst) then return end
        local AngularErr = HOUND.Utils.Elint.generateAngularError(sensorPrecision)

        local vec = l_mist.vec.sub(dst, src)
        local az = l_math.atan2(vec.z,vec.x) + AngularErr.az
        if az < 0 then
            az = az + PI_2
        end
        if az > PI_2 then
            az = az - PI_2
        end

        local el = (l_math.atan(vec.y/l_math.sqrt(vec.x^2 + vec.z^2)) + AngularErr.el)

        return az,el,vec
    end

    function HOUND.Utils.Elint.getSignalStrength(src, dst, maxDetection)
        if not HOUND.Utils.Dcs.isPoint(src) or not HOUND.Utils.Dcs.isPoint(dst) or not (type(maxDetection) == "number" and maxDetection > 0) then return 0 end
        local dist = l_mist.utils.get3DDist(src,dst)
        local rng = (dist/maxDetection)
        return 1/(rng*rng)
    end

    function HOUND.Utils.Elint.getActiveRadars(instanceCoalition)
        local Radars = {}
        if instanceCoalition == nil then return Radars end

        for _,coalitionName in pairs(coalition.side) do
            if coalitionName ~= instanceCoalition then
                for _,CategoryId in pairs({Group.Category.GROUND,Group.Category.SHIP}) do
                    for _,group in pairs(coalition.getGroups(coalitionName, CategoryId)) do
                        for _,unit in pairs(group:getUnits()) do
                            if (unit:isExist() and unit:isActive() and unit:getRadar()) then
                                table.insert(Radars, unit:getName()) -- insert the name
                            end
                        end
                    end
                end
            end
        end
        return Radars
    end

    function HOUND.Utils.Elint.getActiveRadarsInGroup(GroupName)
        local Radars = {}
        if GroupName == nil then return Radars end
        local group = Group.getByName(GroupName)
        if not HOUND.Utils.Dcs.isGroup(group) then return Radars end
        for _,unit in pairs(group:getUnits()) do
            if (unit:isExist() and unit:isActive() and unit:getRadar()) then
                table.insert(Radars, unit:getName()) -- insert the name
            end
        end
        return Radars
    end

    function HOUND.Utils.Elint.getRwrContacts(platform)
        if not HOUND.Utils.Dcs.isUnit(platform) and not platform:hasSensors(Unit.SensorType.RWR) then return {} end
        local radars = {}
        local platformCoalition = platform:getCoalition()
        local contacts = platform:getController():getDetectedTargets(Controller.Detection.RWR)
        for _,unit in contacts do
            if unit:getCoalition() ~= platformCoalition and unit:getRadar() then
                table.insert(radars,unit:getName())
            end
        end
        return radars
    end

    function HOUND.Utils.Vector.getUnitVector(Theta,Phi)
        if not Theta then
            return {x=0,y=0,z=0}
        end
        Phi = Phi or 0
        local unitVector = {
                x = l_math.cos(Phi)*l_math.cos(Theta),
                z = l_math.cos(Phi)*l_math.sin(Theta),
                y = l_math.sin(Phi)
            }
        return unitVector
    end

    function HOUND.Utils.Vector.getRandomVec2(maxError)
        if type(maxError) ~= 'number' or maxError == 0 then return {x=0,y=0,z=0} end
        local stddev = maxError / 2
        local Magnitude = l_math.sqrt(-2 * l_math.log(l_math.random())) * stddev
        local Theta = PI_2 * l_math.random()
        local epsilon = HOUND.Utils.Vector.getUnitVector(Theta)
        for axis,value in pairs(epsilon) do
            epsilon[axis] = value * Magnitude
        end
        return epsilon
    end

    function HOUND.Utils.Vector.getRandomVec3(maxError)
        if type(maxError) ~= 'number' or maxError == 0 then return {x=0,y=0,z=0} end
        local stddev = maxError / 2
        local Magnitude = l_math.sqrt(-2 * l_math.log(l_math.random())) * stddev
        local Theta = PI_2 * l_math.random()
        local Phi = PI_2 * l_math.random()

        local epsilon = HOUND.Utils.Vector.getUnitVector(Theta,Phi)
        for axis,value in pairs(epsilon) do
            epsilon[axis] = value * Magnitude
        end
        return epsilon
    end

    function HOUND.Utils.Zone.listDrawnZones()
        local zoneNames = {}
        local base = _G.env.mission
        if not base or not base.drawings or not base.drawings.layers then return zoneNames end
        for _,drawLayer in pairs(base.drawings.layers) do
            if type(drawLayer["objects"]) == "table" then
                for _,drawObject in pairs(drawLayer["objects"]) do
                    if drawObject["primitiveType"] == "Polygon" and (HOUND.setContainsValue({"free","rect","oval"},drawObject["polygonMode"])) then
                        table.insert(zoneNames,drawObject["name"])
                    end
                end
            end
        end
        return zoneNames
    end

    function HOUND.Utils.Zone.getDrawnZone(zoneName)
        if type(zoneName) ~= "string" then return nil end
        if not _G.env.mission.drawings or not _G.env.mission.drawings.layers then return nil end
        for _,drawLayer in pairs(_G.env.mission.drawings.layers) do
            if type(drawLayer["objects"]) == "table" then
                for _,drawObject in pairs(drawLayer["objects"]) do
                    if drawObject["name"] == zoneName and drawObject["primitiveType"] == "Polygon" then
                        local points = {}
                        local theta = nil
                        if drawObject["polygonMode"] == "free" and HOUND.Length(drawObject["points"]) >2 then
                            points = l_mist.utils.deepCopy(drawObject["points"])
                            table.remove(points)
                        end
                        if drawObject["polygonMode"] == "rect" then
                            theta = l_math.rad(drawObject["angle"])
                            local w,h = drawObject["width"],drawObject["height"]

                            table.insert(points,{x=h/2,y=w/2})
                            table.insert(points,{x=-h/2,y=w/2})
                            table.insert(points,{x=-h/2,y=-w/2})
                            table.insert(points,{x=h/2,y=-w/2})
                        end
                        if drawObject["polygonMode"] == "oval" then
                            theta = l_math.rad(drawObject["angle"])
                            local r1,r2 = drawObject["r1"],drawObject["r2"]
                            local numPoints = 16
                            local angleStep = PI_2/numPoints

                            for i = 1, numPoints do
                                local pointAngle = PI_2 - (i * angleStep)
                                local x = r1 * l_math.cos(pointAngle)
                                local y = r2 * l_math.sin(pointAngle)
                                table.insert(points,{x=x,y=y})
                            end
                        end
                        if theta then
                            for _,point in pairs(points) do
                                local x = point.x
                                local y = point.y
                                point.x = x * l_math.cos(theta) - y * l_math.sin(theta)
                                point.y = x * l_math.sin(theta) + y * l_math.cos(theta)
                            end
                        end
                        if HOUND.Length(points) < 3 then return nil end
                        local objectX,objecty = drawObject["mapX"],drawObject["mapY"]
                        for _,point in pairs(points) do
                            point.x = point.x + objectX
                            point.y = point.y + objecty
                        end
                        return points
                    end
                end
            end
        end
        return nil
    end

    function HOUND.Utils.Zone.getGroupRoute(GroupName)
        if type(GroupName) == "string" and HOUND.Utils.Dcs.isGroup(Group.getByName(GroupName)) then
            return HOUND.Utils.Dcs.getGroupPoints(Group.getByName(GroupName):getID())
        end
    end

    function HOUND.Utils.Sort.ContactsByRange(a,b)
        if a.isEWR ~= b.isEWR then
          return b.isEWR and not a.isEWR
        end
        if a.maxWeaponsRange ~= b.maxWeaponsRange then
            return a.maxWeaponsRange > b.maxWeaponsRange
        end
        if a.detectionRange ~= b.detectionRange then
            return a.detectionRange > b.detectionRange
        end
        if a.typeAssigned ~= b.typeAssigned then
            return table.concat(a.typeAssigned) < table.concat(b.typeAssigned)
        end
        if a.typeName ~= b.typeName then
            return a.typeName < b.typeName
        end
        if a.first_seen ~= b.first_seen then
            return a.first_seen > b.first_seen
        end
        if getmetatable(a) == HOUND.Contact.Site then
            return a.gid < b.gid
        end
        return a.uid < b.uid
    end

    function HOUND.Utils.Sort.ContactsById(a,b)
        if  a.uid ~= b.uid then
            return a.uid < b.uid
        end
        return a.maxWeaponsRange > b.maxWeaponsRange
    end

    function HOUND.Utils.Sort.ContactsByPrio(a,b)
        if a.isPrimary ~= b.isPrimary then
            return a.isPrimary and not b.isPrimary
        end
        if a.radarRoles ~= b.radarRoles then
            local aRoles,bRoles = 0,0
            for _,role in pairs(a.radarRoles) do
                aRoles = aRoles + role
            end
            for _,role in pairs(b.radarRoles) do
                bRoles = bRoles + role
            end
            return aRoles > bRoles
        end
        return a.uid < b.uid
    end

    function HOUND.Utils.Sort.sectorsByPriorityLowFirst(a,b)
        return a:getPriority() > b:getPriority()
    end

    function HOUND.Utils.Sort.sectorsByPriorityLowLast(a,b)
        return a:getPriority() < b:getPriority()
    end

    function HOUND.Utils.Filter.groupsByPrefix(prefix)
        if type(prefix) ~= "string" then return {} end
        local groups = {}
        for groupName, _ in pairs(HOUND.Utils.Dcs.getGroupNames(prefix)) do
            local dcsObject = Group.getByName(groupName)
            if HOUND.Utils.Dcs.isGroup(dcsObject) then
                groups[groupName] = dcsObject
            end
        end
        return groups
    end

    function HOUND.Utils.Filter.unitsByPrefix(prefix)
        if type(prefix) ~= "string" then return {} end
        local units = {}
        for unitName, _ in pairs(HOUND.Utils.Dcs.getUnitNames(prefix)) do
            local dcsUnit = Unit.getByName(unitName)
            if HOUND.Utils.Dcs.isUnit(dcsUnit) then
                units[unitName] = dcsUnit
            end
        end
        return units
    end

    function HOUND.Utils.Filter.staticObjectsByPrefix(prefix)
        if type(prefix) ~= "string" then return {} end
        local objects = {}
        for objectName, _ in pairs(HOUND.Utils.Dcs.getStaticObjectNames(prefix)) do
            local dcsObject = StaticObject.getByName(objectName)
            if HOUND.Utils.Dcs.isStaticObject(dcsObject) then
                objects[objectName] = dcsObject
            end
        end
        return objects
    end
end
do
    local l_mist = HOUND.Mist
    local l_math = math
    local l_grpc = GRPC
    local l_stts = HoundTTS or STTS
    local l_houndTTS = HoundTTS
    local PI_2 = 2*l_math.pi

    HOUND.Utils.TTS = {}

    function HOUND.Utils.TTS.isAvailable()
        for _,engine in ipairs(HOUND.TTS_ENGINE) do
            if engine == "HOUND" and l_houndTTS ~= nil then return true end
            if engine == "GRPC" and (l_grpc ~= nil and type(l_grpc.tts) == "function") then return true end
            if engine == "STTS" and l_stts ~= nil then return true end
        end
        return false
    end

    function HOUND.Utils.TTS.getdefaultModulation(freq)
        if not freq then return "AM" end
        if tonumber(freq) ~= nil then
            freq = tonumber(freq)
            if freq < 90 or (freq > 1000000 and freq < (90 * 1000000)) then
                return "FM"
            else
                return "AM"
            end
        end
        if type(freq) == "string" then
            freq = string.split(freq,",")
        end
        if type(freq) == "table" then
            local retval = {}
            for _,frequency in ipairs(freq) do
                table.insert(retval,HOUND.Utils.TTS.getdefaultModulation(tonumber(frequency)))
            end
            return table.concat(retval,",")
        end
        return "AM"
    end

    function HOUND.Utils.TTS.Transmit(msg,coalitionID,args,transmitterPos)
        if not HOUND.Utils.TTS.isAvailable() then return end
        if msg == nil then return end
        if coalitionID == nil then return end

        if args.freq == nil then return end
        args.volume = args.volume or "1.0"
        args.name = args.name or "Hound"
        args.gender = args.gender or "female"
        if type(args.tts_engine) ~= "string" or not HOUND.setContainsValue(HOUND.TTS_ENGINE,args.tts_engine) then
            for _,engine in ipairs(HOUND.TTS_ENGINE) do
                if engine == "HOUND" and (l_houndTTS ~= nil and type(l_houndTTS.Transmit) == "function") then
                    args.tts_engine = engine
                    break
                end
                if engine == "GRPC" and (l_grpc ~= nil and type(l_grpc.tts) == "function") then
                    args.tts_engine = engine
                    break
                end
                if engine == "STTS" and l_stts ~= nil then
                    args.tts_engine = engine
                    break
                end
            end
        end
        if args.tts_engine == "STTS" then
            return HOUND.Utils.TTS.TransmitSTTS(msg,coalitionID,args,transmitterPos)
        end
        if args.tts_engine == "GRPC" then
            return HOUND.Utils.TTS.TransmitGRPC(msg,coalitionID,args,transmitterPos)
        end
        if args.tts_engine == "HOUND" then
            return HOUND.Utils.TTS.TransmitHound(msg,coalitionID,args,transmitterPos)
        end
    end

    function HOUND.Utils.TTS.TransmitSTTS(msg,coalitionID,args,transmitterPos)
        args.modulation = args.modulation or HOUND.Utils.TTS.getdefaultModulation(args.freq)
        args.culture = args.culture or "en-US"
        return l_stts.TextToSpeech(msg,args.freq,args.modulation,args.volume,args.name,coalitionID,transmitterPos,args.speed,args.gender,args.culture,args.voice,args.googletts,args.azurecreds)
    end

    function HOUND.Utils.TTS.TransmitHound(msg,coalitionID,args,transmitterPos)
        local transmitter_params = {
            name = args.name,
            freq = args.freq,
            modulation = args.modulation or HOUND.Utils.TTS.getdefaultModulation(args.freq),
            point = transmitterPos,
            coalition = coalitionID,
            transmitter = args.transmitter or "srs"
        }
        local provider_params = {
            provider = args.provider or "sapi",
            culture = args.culture or "en-US",
            gender = args.gender or "female",
            voice = args.voice,
            speed = args.speed,
            volume = args.volume or "1.0"
        }

        if args.googletts then
            provider_params.provider = "google"
        end

        if provider_params.provider == "piper" then
            provider_params.voice = args.voice or "en_US-lessac-low"
            provider_params.speaker = args.speaker
        elseif provider_params.provider == "google" or provider_params.provider == "gcloud" then
            provider_params.voice = args.voice or "google-auto"
        elseif provider_params.provider == "aws" or provider_params.provider == "polly" then
            provider_params.voice = args.voice or "Joanna"
            provider_params.engine = provider_params.engine or "standard"
        elseif provider_params.provider == "azure" then
            provider_params.voice = args.voice
        elseif provider_params.provider == "elevenlabs" then
            provider_params.voice = args.voice or "pNInz6obpgDQGcFmaJgB" -- Default to "Adam"
        end

        return l_houndTTS.Transmit(msg,transmitter_params,provider_params)
    end

    function HOUND.Utils.TTS.TransmitGRPC(msg,coalitionID,args,transmitterPos)
        local VOLUME = {"default","x-slow", "slow", "medium", "fast", "x-fast"}
        local ssml_msg = msg

        local grpc_ttsArgs = {
            srsClientName = args.name,
            coalition = HOUND.Utils.getCoalitionString(coalitionID):lower(),
        }
        if type(transmitterPos) == "table" then
            grpc_ttsArgs.position = {}
            grpc_ttsArgs.position.lat, grpc_ttsArgs.position.lon, grpc_ttsArgs.position.alt = coord.LOtoLL( transmitterPos )
        end
        if type(args.provider) == "table" then
            grpc_ttsArgs.provider = args.provider
        end

        local readSpeed = 1.0
        if args.speed ~= 0 then
            if args.speed > 10 then
                readSpeed = HOUND.Utils.Mapping.linear(args.speed,50,250,0.5,2.5,true)
            else
                if args.speed > 0 then
                    readSpeed = HOUND.Utils.Mapping.linear(args.speed,0,10,1.0,2.5,true)
                else
                    readSpeed = HOUND.Utils.Mapping.linear(args.speed,-10,0,0.5,1.0,true)
                end
            end
        end

        local ssml_prosody = ""
        if readSpeed ~= 1.0  then
            ssml_prosody = ssml_prosody .. " rate='"..readSpeed.."'"
        end

        if args.volume ~= 1.0 then
            local volume = ""

            if HOUND.setContainsValue(VOLUME,args.volume) then
                volume = args.volume
            end

            if type(args.volume)=="number" then
                if args.volume ~= 0 then
                    volume = (args.volume*100)-100 .. "%"
                    if args.volume > 1 then
                        volume = "+" .. volume
                    end
                else
                    volume = "slient"
                end
            end

            if string.len(volume) > 0 then
                ssml_prosody = ssml_prosody .. " volume='"..volume.."'"
            end
        end
        if string.len(ssml_prosody) > 0 then
            ssml_msg = table.concat({"<prosody",ssml_prosody,">",ssml_msg,"</prosody>"},"")
        end

        local ssml_voice = ""
        if args.voice then
            ssml_voice = ssml_voice.." name='"..args.voice.."'"
        else
            if args.gender then
                ssml_voice = ssml_voice.." gender='"..args.gender.."'"
            end
            if args.culture then
                ssml_voice = ssml_voice.." language='"..args.culture.."'"
            end
        end

        if string.len(ssml_voice) > 0 then
            ssml_msg = table.concat({"<voice",ssml_voice,">",ssml_msg,"</voice>"},"")
        end

        local freqs = string.split(args.freq,",")

        for _,freq in ipairs(freqs) do
            freq = math.ceil(freq * 1000000)
            l_grpc.tts(ssml_msg, freq, grpc_ttsArgs)
        end
        return HOUND.Utils.TTS.getReadTime(msg) / readSpeed -- read speed > 1.0 is fast
    end

    function HOUND.Utils.TTS.getTtsTime(timestamp)
        if timestamp == nil then timestamp = timer.getAbsTime() end
        local DHMS = l_mist.time.getDHMS(timestamp)
        local hours = DHMS.h
        local minutes = DHMS.m
        if hours == 0 then
            hours = HOUND.DB.PHONETICS["0"]
        else
            hours = string.format("%02d",hours)
        end

        if minutes == 0 then
            minutes = "hundred"
        else
            minutes = string.format("%02d",minutes)
        end

        return hours .. " " .. minutes .. " Local"
    end

    function HOUND.Utils.TTS.getVerbalConfidenceLevel(confidenceRadius)
        if confidenceRadius == 0.1 then return "Precise" end

        local score={
            "Very High", -- 500
            "High", -- 1000
            "Medium", -- 1500
            "Low", -- 2000
            "Low", -- 2500
            "Very Low", -- 3000
            "Very Low", -- 3500
            "Very Low", -- 4000
            "Very Low", -- 4500
            "Unactionable", -- 5000
        }
        return score[l_math.min(#score,l_math.max(1,l_math.floor(confidenceRadius/500)+1))]
    end

    function HOUND.Utils.TTS.getVerbalContactAge(timestamp,isSimple,NATO)
        local ageSeconds = HOUND.Utils.absTimeDelta(timestamp,timer.getAbsTime())

        if isSimple then
            if NATO then
                if ageSeconds < 16 then return "Active" end
                if ageSeconds < HOUND.CONTACT_TIMEOUT then return "Down" end
                return "Asleep"
            end
            if ageSeconds < 16 then return "Active" end
            if ageSeconds <= 90 then return "very recent" end
            if ageSeconds <= 180 then return "recent" end
            if ageSeconds <= 300 then return "relevant" end
            return "stale"
        end
        local DHMS = l_mist.time.getDHMS(ageSeconds)
        if ageSeconds < 60 then return tostring(l_math.floor(DHMS.s)) .. " seconds" end
        if ageSeconds < 7200 then return tostring(l_math.floor(DHMS.h)*60+l_math.floor(DHMS.m)) .. " minutes" end
        return tostring(l_math.floor(DHMS.h)) .. " hours, " .. tostring(l_math.floor(DHMS.m)) .. " minutes"
    end

    function HOUND.Utils.TTS.DecToDMS(cood,minDec,padDeg)
        local DMS = HOUND.Utils.DecToDMS(cood)
        local strTab = {
            l_math.abs(DMS.d) .. " degrees",
            string.format("%02d",DMS.m) .. " minutes",
            string.format("%02d",DMS.s) .. " seconds"
        }
        if padDeg == true then
            strTab[1] = string.format("%03d",l_math.abs(DMS.d)) .. " degrees"
        end
        if minDec == true then
            strTab[2] = string.format("%02d",DMS.m)
            strTab[3] = HOUND.Utils.TTS.toPhonetic( "." .. string.format("%03d",DMS.sDec)) .. " minutes"
        end
        return table.concat(strTab,", ")
    end

    function HOUND.Utils.TTS.getVerbalLL(lat,lon,minDec)
        minDec = minDec or false
        local hemi = HOUND.Utils.getHemispheres(lat,lon,true)
        return hemi.NS .. ", " .. HOUND.Utils.TTS.DecToDMS(lat,minDec)  ..  ", " .. hemi.EW .. ", " .. HOUND.Utils.TTS.DecToDMS(lon,minDec,true)
    end

    function HOUND.Utils.TTS.toPhonetic(str)
        local retval = ""
        str = string.upper(tostring(str))
        for i=1, string.len(str) do
            local char = HOUND.DB.PHONETICS[string.sub(str, i, i)] or ""
            retval = retval .. char .. " "
        end
        return retval:match( "^%s*(.-)%s*$" ) -- return and strip trailing whitespaces
    end

    function HOUND.Utils.TTS.getReadTime(length,speed,googleTTS)
        if length == nil then return nil end
        local maxRateRatio = 3 -- can be chaned to 5 if windows TTSrate is up to 5x not 4x

        speed = speed or 1.0
        googleTTS = googleTTS or false

        local speedFactor = 1.0
        if googleTTS then
            speedFactor = speed
        else
            if speed ~= 0 then
                speedFactor = l_math.abs(speed) * (maxRateRatio - 1) / 10 + 1
            end
            if speed < 0 then
                speedFactor = 1/speedFactor
            end
        end

        local wpm = l_math.ceil(100 * speedFactor)
        local cps = l_math.floor((wpm * 5)/60)

        if type(length) == "string" then
            length = string.len(length)
        end

        return l_math.ceil(length/cps)
    end

    function HOUND.Utils.TTS.simplfyDistance(distanceM)
        local distanceUnit = "meters"
        local distance = HOUND.Utils.roundToNearest(distanceM,50) or 0
        if distance >= 1000 then
            distance = string.format("%.1f",tostring(HOUND.Utils.roundToNearest(distanceM,100)/1000))
            distanceUnit = "kilometers"
        end
        return distance .. " " .. distanceUnit
    end
end    --- HOUND.Utils
do
    local l_mist = HOUND.Mist
    local l_math = math
    local PI_2 = 2*l_math.pi

    HOUND.Utils.Polygon ={}
    HOUND.Utils.Cluster = {}

    function HOUND.Utils.Polygon.threatOnSector(polygon,point, radius)
        if type(polygon) ~= "table" or HOUND.Length(polygon) < 3 or not HOUND.Utils.Dcs.isPoint(l_mist.utils.makeVec3(polygon[1])) then
            return
        end
        if not HOUND.Utils.Dcs.isPoint(point) then
            return
        end
        local inPolygon = l_mist.pointInPolygon(point,polygon)
        local intersectsPolygon = inPolygon

        if radius ~= nil and radius > 0 and l_mist.shape ~= nil then
            local circle={point=point,radius=radius}
            intersectsPolygon = l_mist.shape.insideShape(circle,polygon)
        end
        return inPolygon,intersectsPolygon
    end

    function HOUND.Utils.Polygon.azMinMax(poly,refPos)
        if not HOUND.Utils.Dcs.isPoint(refPos) or type(poly) ~= "table" or HOUND.Length(poly) < 2 or l_mist.pointInPolygon(refPos,poly) then
            return
        end

        local points = l_mist.utils.deepCopy(poly)
        for _,pt in pairs(points) do
            pt.refAz = l_mist.utils.getDir(l_mist.vec.sub(pt,refPos))
        end

        table.sort(points,function (a,b) return (a.refAz+PI_2) < (b.refAz+PI_2) end)
        local leftMost = table.remove(points,1)
        local rightMost = table.remove(points)
        return HOUND.Utils.angleDeltaRad(leftMost.refAz,rightMost.refAz),(leftMost),(rightMost)
    end

    function HOUND.Utils.Cluster.gaussianKernel(value,bandwidth)
        return (1/(bandwidth*l_math.sqrt(2*l_math.pi))) * l_math.exp(-0.5*((value / bandwidth))^2)
    end

    function HOUND.Utils.Cluster.getDeltaSubsetPercent(Table,referencePos,NthPercentile,returnRelative)
        local t = l_mist.utils.deepCopy(Table)
        local len_t = HOUND.Length(t)
        t = HOUND.Utils.Geo.setHeight(t)
        if not referencePos then
            referencePos = l_mist.getAvgPoint(t)
        end
        for _,pt in ipairs(t) do
            pt.dist = l_mist.utils.get2DDist(referencePos,pt)
        end
        table.sort(t,function(a,b) return a.dist < b.dist end)

        local percentile = l_math.floor(len_t*NthPercentile)
        local NumToUse = l_math.max(l_math.min(2,len_t),percentile)
        local returnTable = {}
        for i = 1, NumToUse  do
            table.insert(returnTable,t[i])
        end
        if returnRelative then
            for i = 1,#returnTable do
                returnTable[i] = l_mist.vec.sub(returnTable[i],referencePos)
            end
        end

        return returnTable
    end

    function HOUND.Utils.Cluster.WeightedCentroid(PosList)
        local sumWeights = 0
        local estimate = {z=0,x=0}
        for _,pos in ipairs(PosList) do
            if HOUND.Utils.Dcs.isPoint(pos) and pos.score > 0 then
                local w = pos.score
                sumWeights = sumWeights + w
                estimate.z = estimate.z + (w * (pos.z - estimate.z)) / sumWeights
                estimate.x = estimate.x + (w * (pos.x - estimate.x)) / sumWeights
            end
        end
        estimate.y = land.getHeight({x=estimate.x, y=estimate.z}) or 0
        return estimate
    end
end    --- HOUND.EventHandler
do
    HOUND.EventHandler = {
        idx = 0,
        subscribers = {},
        _internalSubscribers = {},
        subscribeOn = {}
    }

    HOUND.EventHandler.__index = HOUND.EventHandler

    function HOUND.EventHandler.addEventHandler(handler)
        if type(handler) == "table" then
            HOUND.EventHandler.subscribers[handler] = handler
        end
    end

    function HOUND.EventHandler.removeEventHandler(handler)
        HOUND.EventHandler.subscribers[handler] = nil
        for eventType,_ in pairs(HOUND.EventHandler.subscribeOn) do
            HOUND.EventHandler.subscribeOn[eventType][handler] = nil
        end
    end

    function HOUND.EventHandler.addInternalEventHandler(handler)
        if type(handler) == "table" then
            HOUND.EventHandler._internalSubscribers[handler] = handler
        end
    end

    function HOUND.EventHandler.removeInternalEventHandler(handler)
        if HOUND.setContains(HOUND.EventHandler._internalSubscribers,handler) then
            HOUND.EventHandler._internalSubscribers[handler] = nil
        end
    end

    function HOUND.EventHandler.on(eventType,handler)
        if type(handler) == "function" then
            if not HOUND.EventHandler.subscribeOn[eventType] then
                HOUND.EventHandler.subscribeOn[eventType] = {}
            end
            HOUND.EventHandler.subscribeOn[eventType][handler] = handler
        end
    end

    function HOUND.EventHandler.onHoundEvent(event)
        for _, handler in pairs(HOUND.EventHandler._internalSubscribers) do
            if handler and getmetatable(handler) == HoundElint and handler:getId() == event.houndId then
                if handler.onHoundInternalEvent and type(handler.onHoundInternalEvent) == "function" then
                    handler:onHoundInternalEvent(event)
                end
                if handler.onHoundEvent and type(handler.onHoundEvent) == "function" then
                    handler:onHoundEvent(event)
                end
            end
        end
        for _, handler in pairs(HOUND.EventHandler.subscribers) do
            if handler.onHoundEvent and type(handler.onHoundEvent) == "function" then
                handler:onHoundEvent(event)
            end
        end
    end

    function HOUND.EventHandler.publishEvent(event)
        if not event.time then
            event.time = timer.getTime()
        end
        HOUND.EventHandler.onHoundEvent(event)
    end

    function HOUND.EventHandler.getIdx()
        HOUND.EventHandler.idx = HOUND.EventHandler.idx + 1
        return  HOUND.EventHandler.idx
    end
end
do
    HOUND.Contact.Base = {}
    HOUND.Contact.Base.__index = HOUND.Contact.Base

    local HoundUtils = HOUND.Utils

    function HOUND.Contact.Base:New(DcsObject,HoundCoalition)
        if not DcsObject or type(DcsObject) ~= "table" or not DcsObject.getName or not HoundCoalition then
            HOUND.Logger.warn("failed to create HOUND.Contact instance")
            return
        end
        local instance = {}
        setmetatable(instance, HOUND.Contact.Base)
        instance.DcsObject = DcsObject
        instance.DcsGroupName = nil
        instance.DcsObjectName = nil
        instance.typeAssigned = {"Unknown"}

        instance.pos = {
            p = nil,
            grid = nil,
            LL = {
                lat = nil,
                lon = nil,
            },
            be = {
                brg = nil,
                rng = nil
            }
        }
        instance.uncertenty_data = nil
        instance.last_seen = timer.getAbsTime()
        instance.first_seen = timer.getAbsTime()
        instance.maxWeaponsRange = 0
        instance.detectionRange = 0

        instance._platformCoalition = HoundCoalition
        instance.primarySector = "default"
        instance.threatSectors = {
            default = true
        }
        instance.state = nil
        instance.preBriefed = false
        instance.events = {}
        instance._markpoints = {
            pos = HoundUtils.Marker.create(),
            area = HoundUtils.Marker.create()
        }
        return instance
    end

    function HOUND.Contact.Base:destroy()
        HOUND.Logger.error("HOUND.Contact.Base:destroy() prototype envoked. please override")
    end

    function HOUND.Contact.Base:getDcsGroupName()
        return self.DcsGroupName
    end

    function HOUND.Contact.Base:getDcsName()
        return self.DcsObjectName
    end

    function HOUND.Contact.Base:getDcsObject()
        return self.DcsObject or self.DcsObjectName
    end
    function HOUND.Contact.Base:getLastSeen()
        return HoundUtils.absTimeDelta(self.last_seen)
    end

    function HOUND.Contact.Base:getObject()
        return self.DcsObject
    end
    function HOUND.Contact.Base:hasPos()
        return HoundUtils.Dcs.isPoint(self.pos.p)
    end

    function HOUND.Contact.Base:getMaxWeaponsRange()
        return self.maxWeaponsRange
    end

    function HOUND.Contact.Base:getRadarDetectionRange()
        return self.detectionRange
    end

    function HOUND.Contact.Base:getTypeAssigned()
        return table.concat(self.typeAssigned," or ")
    end

    function HOUND.Contact.Base:getDesignation(NATO)
        if not NATO then return self:getTypeAssigned() end
        local natoDesignation = string.gsub(self:getTypeAssigned(),"(SA)-",'')
        if natoDesignation == "Naval" then
            natoDesignation = self:getType()
        end
        return natoDesignation
    end

    function HOUND.Contact.Base:getNatoDesignation()
        return self:getDesignation(true)
    end

    function HOUND.Contact.Base:isActive()
        return self:getLastSeen()/16 < 1.0
    end

    function HOUND.Contact.Base:isRecent()
        return self:getLastSeen()/120 < 1.0
    end

    function HOUND.Contact.Base:isAccurate()
        return self.preBriefed
    end

    function HOUND.Contact.Base:getPreBriefed()
        return self.preBriefed
    end

    function HOUND.Contact.Base:setPreBriefed(state)
        if type(state) == "boolean" then
            self.preBriefed = state
            if not self.preBriefed then
                if type(self.detected_by) == "table" then
                    for i,v in ipairs(self.detected_by) do
                        if v == "External" then
                            table.remove(self.detected_by,i)
                            break
                        end
                    end
                end
            end
        end
    end

    function HOUND.Contact.Base:isTimedout()
        return self:getLastSeen() > HOUND.CONTACT_TIMEOUT
    end

    function HOUND.Contact.Base:getState()
        return self.state
    end

    function HOUND.Contact.Base:queueEvent(eventId)
        if eventId == HOUND.EVENTS.NO_CHANGE then return end
        local event = {
            id = eventId,
            initiator = self,
            time = timer.getTime()
        }
        table.insert(self.events,event)
    end

    function HOUND.Contact.Base:getEventQueue()
        return self.events
    end

    function HOUND.Contact.Base:getPrimarySector()
        return self.primarySector
    end

    function HOUND.Contact.Base:getSectors()
        return self.threatSectors
    end

    function HOUND.Contact.Base:isInSector(sectorName)
        return self.threatSectors[sectorName] or false
    end

    function HOUND.Contact.Base:updateDefaultSector()
        self.threatSectors[self.primarySector] = true
        if self.primarySector == "default" then return end
        for k,v in pairs(self.threatSectors) do
            if k ~= "default" and v == true then
                self.threatSectors["default"] = false
                return
            end
        end
        self.threatSectors["default"] = true
    end

    function HOUND.Contact.Base:updateSector(sectorName,inSector,threatsSector)
        if inSector == nil and threatsSector == nil then
            return
        end
        self.threatSectors[sectorName] = threatsSector or false

        if inSector and self.primarySector ~= sectorName then
            self.primarySector = sectorName
            self.threatSectors[sectorName] = true
        end
        self:updateDefaultSector()
    end

    function HOUND.Contact.Base:addSector(sectorName)
        self.threatSectors[sectorName] = true
        self:updateDefaultSector()
    end

    function HOUND.Contact.Base:removeSector(sectorName)
        if self.threatSectors[sectorName] then
            self.threatSectors[sectorName] = false
            self:updateDefaultSector()
        end
    end

    function HOUND.Contact.Base:isThreatsSector(sectorName)
        return self.threatSectors[sectorName] or false
    end

    function HOUND.Contact.Base:removeMarkers()
        for _,marker in pairs(self._markpoints) do
            marker:remove()
        end
    end
end--- HOUND.Contact.Estimator
do
    local l_math = math
    local TwoPI = 2 * l_math.pi
    local HalfPi = l_math.pi / 2
    local HoundUtils = HOUND.Utils
    local matrix = HOUND.Matrix

    HOUND.Contact.Estimator = {}
    HOUND.Contact.Estimator.__index = HOUND.Contact.Estimator

    HOUND.Contact.Estimator.Kalman = {}

    function HOUND.Contact.Estimator.accuracyScore(err)
        local score = 0
        if type(err) == "number" then
            score = HoundUtils.Mapping.linear(err, 0, 100000, 1, 0, true)
            score = HoundUtils.Cluster.gaussianKernel(score, 0.2)
        end
        if type(score) == "number" then
            return score
        else
            return 0
        end
    end

    function HOUND.Contact.Estimator.Kalman.posFilter()
        local Kalman = {}

        Kalman.P = {
            x = 0.5,
            z = 0.5
        }

        Kalman.estimated = {}

        Kalman.update = function(self, datapoint)
            if type(self.estimated.p) ~= "table" and HoundUtils.Dcs.isPoint(datapoint) then
                self.estimated.p = {
                    x = datapoint.x,
                    z = datapoint.z,
                    y = datapoint.y
                }
            end

            if type(datapoint.err.score) ~= "table" then
                return self.estimated.p
            end
            self.P.x = self.P.x + math.sqrt(datapoint.err.score.x)
            self.P.z = self.P.z + math.sqrt(datapoint.err.score.z)

            local Kx = self.P.x / (self.P.x + (datapoint.err.score.x))
            local Kz = self.P.z / (self.P.z + (datapoint.err.score.z))

            self.estimated.p.x = self.estimated.p.x + (Kx * (datapoint.x - self.estimated.p.x))
            self.estimated.p.z = self.estimated.p.z + (Kz * (datapoint.z - self.estimated.p.z))

            self.P.x = (1 - Kx) * self.P.x
            self.P.z = (1 - Kz) * self.P.z

            self.estimated.p = HoundUtils.Geo.setHeight(self.estimated.p)
            return self.estimated.p
        end

        Kalman.get = function(self)
            return self.estimated.p
        end

        return Kalman
    end

    function HOUND.Contact.Estimator.Kalman.AzFilter(maxError)
        local Kalman = {}
        Kalman.P = 0.5
        local sigma = maxError / 2
        Kalman.noiseVariance = sigma * sigma

        Kalman.estimated = nil

        Kalman.update = function(self, newAz, predictedAz, processNoise)
            if not self.estimated then
                self.estimated = newAz
            end
            local predAz = self.estimated
            local noiseVar = self.noiseVariance
            if type(predictedAz) == "number" then
                predAz = predictedAz
            end
            if type(processNoise) == "number" then
                local sigma_p = processNoise / 2
                noiseVar = sigma_p * sigma_p
            end

            self.P = self.P + noiseVar -- add process noise as variance
            local K = self.P / (self.P + self.noiseVariance)
            local deltaAz = newAz - predAz
            self.estimated = ((self.estimated + K * (deltaAz)) + TwoPI) % TwoPI
            self.P = (1 - K) * self.P
        end

        Kalman.get = function(self)
            return self.estimated
        end

        return Kalman
    end

    function HOUND.Contact.Estimator.Kalman.AzElFilter()
        local Kalman = {}
        Kalman.K = {
            Az = 0,
            El = 0
        }
        Kalman.P = {
            Az = 1,
            El = 1
        }
        Kalman.estimated = {
            pos = nil,
            Az = nil,
            El = nil
        }

        Kalman.reset = function(self)
            self.P = {
                Az = 1,
                El = 1
            }
        end

        Kalman.update = function(self, datapoint)
            if not self.estimated.pos then
                self.estimated.Az = (1 / self.P.Az) * datapoint.az
                self.estimated.El = (1 / self.P.El) * datapoint.el
                self.estimated.pos = HoundUtils.Geo.getProjectedIP(datapoint.platformPos, self.estimated.Az,
                    self.estimated.El)
                return self.estimated
            end
            local prediction = self:predict(datapoint)

            local errEstimate = {
                Az = datapoint.platformPrecision,
                El = datapoint.platformPrecision
            }

            self.K.Az = self.P.Az / (self.P.Az + errEstimate.Az)
            self.K.El = self.P.El / (self.P.El + errEstimate.El)

            self.estimated.Az = self.estimated.Az + (self.K.Az * (datapoint.az - prediction.Az))
            self.estimated.El = self.estimated.El + (self.K.El * (datapoint.el - prediction.El))
            self.estimated.pos = HoundUtils.Geo.getProjectedIP(datapoint.platformPos, self.estimated.Az,
                self.estimated.El)

            self.P.Az = (1 - self.K.Az)
            self.P.El = (1 - self.K.El)

            return self.estimated
        end

        Kalman.predict = function(self, datapoint)
            local prediction = {}
            prediction.Az, prediction.El = HoundUtils.Elint.getAzimuth(datapoint.platformPos, self.estimated.pos, 0)
            return prediction
        end

        Kalman.getValue = function(self)
            return self.estimated
        end

        return Kalman
    end

    HOUND.Contact.Estimator.UPLKF = {}
    HOUND.Contact.Estimator.UPLKF.__index = HOUND.Contact.Estimator.UPLKF

    function HOUND.Contact.Estimator.UPLKF:create(p0, v0, timestamp, initialPosError, isMobile)
        if not HoundUtils.Dcs.isPoint(p0) then return nil end
        local instance = {}
        setmetatable(instance, HOUND.Contact.Estimator.UPLKF)
        instance.t0 = timestamp or timer.getAbsTime()
        instance.mobile = isMobile or false
        v0 = v0 or { z = 0, x = 0 }

        instance.state = matrix({
            { p0.x },
            { p0.z },
            { v0.x },
            { v0.z }
        })
        local position_accuracy = initialPosError or 10000
        position_accuracy = l_math.min(position_accuracy, 10000)
        local velocity_accuracy = instance.mobile and 30 or 1
        instance.P = matrix({
            { l_math.pow(position_accuracy, 2), 0,                                0,                                0 },
            { 0,                                l_math.pow(position_accuracy, 2), 0,                                0 },
            { 0,                                0,                                l_math.pow(velocity_accuracy, 2), 0 },
            { 0,                                0,                                0,                                l_math.pow(velocity_accuracy, 2) }
        })

        if HOUND.KALMAN_DEBUG and  HOUND.DEBUG then
            instance.marker = HoundUtils.Marker.create()
            trigger.action.outText("new KF: x:" .. instance.state[2][1] .. "| y: " .. instance.state[1][1], 20)
        end
        return instance
    end

    function HOUND.Contact.Estimator.UPLKF:getEstimatedPos(state)
        local X_k = state or self.state
        local pos = { x = X_k[1][1], z = X_k[2][1] }
        if HoundUtils.Dcs.isPoint(pos) then
            pos = HoundUtils.Geo.setPointHeight(pos)
            return pos
        end
    end

    function HOUND.Contact.Estimator.UPLKF:getUncertainty(confidence)
        local k = confidence or 2.45

        local P_xx = self.P[1][1]  -- variance in x (North)
        local P_zz = self.P[2][2]  -- variance in z (East)
        local P_xz = self.P[1][2]  -- covariance x-z

        local trace = P_xx + P_zz
        local det = P_xx * P_zz - P_xz * P_xz
        local discriminant = l_math.max(0, trace * trace - 4 * det)  -- ensure non-negative
        local sqrt_disc = l_math.sqrt(discriminant)

        local lambda1 = (trace + sqrt_disc) / 2  -- larger eigenvalue
        local lambda2 = (trace - sqrt_disc) / 2  -- smaller eigenvalue

        local major = k * l_math.sqrt(l_math.max(0, lambda1))
        local minor = k * l_math.sqrt(l_math.max(0, lambda2))

        local theta = 0.5 * l_math.atan2(2 * P_xz, P_xx - P_zz)

        local uncertenty_data = {}
        uncertenty_data.major = l_math.floor(major * 2 + 0.5)  -- full axis length, rounded
        uncertenty_data.minor = l_math.floor(minor * 2 + 0.5)  -- full axis length, rounded
        uncertenty_data.theta = theta
        uncertenty_data.az = l_math.floor(l_math.deg(theta) + 0.5)
        uncertenty_data.r = (uncertenty_data.major + uncertenty_data.minor) / 4

        return uncertenty_data
    end

    function HOUND.Contact.Estimator.UPLKF.normalizeAz(azimuth)
        local bearing = (HalfPi - azimuth + TwoPI) % TwoPI
        return (((bearing) + l_math.pi) % TwoPI) - l_math.pi
    end

    function HOUND.Contact.Estimator.UPLKF.bearingToAzimuth(bearing)
        return (HalfPi - bearing + TwoPI) % TwoPI
    end

    function HOUND.Contact.Estimator.UPLKF:updateMarker()
        local pos = self:getEstimatedPos()
        self.marker:update({
            useLegacyMarker = false
            ,
            pos = pos,
            text = "UB-PLKF",
            coalition = -1
        })
    end

    function HOUND.Contact.Estimator.UPLKF:getF(deltaT)
        local Ft = matrix(4, "I")
        Ft[1][3] = deltaT
        Ft[2][4] = deltaT
        return Ft
    end

    function HOUND.Contact.Estimator.UPLKF:getQ(deltaT)
        local dT = deltaT or 10
        local q_a = self.mobile and 0.5 or 0.01
        local q = q_a * q_a  -- variance

        return matrix({
            { (dT^4)/4 * q, 0,             (dT^3)/2 * q, 0 },
            { 0,            (dT^4)/4 * q,  0,            (dT^3)/2 * q },
            { (dT^3)/2 * q, 0,             (dT^2) * q,   0 },
            { 0,            (dT^3)/2 * q,  0,            (dT^2) * q },
        })
    end

    function HOUND.Contact.Estimator.UPLKF:predictStep(X, P, timestep, Q)
        local F = self:getF(timestep)
        local Q = Q or self:getQ(timestep)
        local x_hat = F * X
        local P_hat = F * P * F:transpose() + Q

        x_hat[3][1] = HoundUtils.Mapping.clamp(x_hat[3][1], -30.0, 30.0)
        x_hat[4][1] = HoundUtils.Mapping.clamp(x_hat[4][1], -30.0, 30.0)
        return x_hat, P_hat
    end

    function HOUND.Contact.Estimator.UPLKF:predict(timestamp)
        timestamp = timestamp or timer.getAbsTime()
        local deltaT = timestamp - self.t0
        self.t0 = timestamp

        self.state, self.P = self:predictStep(self.state, self.P, deltaT)
    end

    function HOUND.Contact.Estimator.UPLKF:update(p0, z, timestamp, z_err)
        timestamp = timestamp or timer.getAbsTime()
        local deltaT = timestamp - self.t0

        local sigma_r = (z_err or l_math.rad(HOUND.MAX_ANGULAR_RES_DEG)) / 2

        local x_hat, P_k
        if deltaT < 0.5 then
            x_hat = self.state
            P_k = self.P
        else
            self.t0 = timestamp  -- only update time reference for significant time steps
            local Q = self:getQ(deltaT)
            x_hat, P_k = self:predictStep(self.state, self.P, deltaT, Q)
        end

        local estimatedPos = self:getEstimatedPos(x_hat)

        local beta_measured = z  -- measured azimuth (with noise)

        local cos_beta_k, sin_beta_k = l_math.cos(beta_measured), l_math.sin(beta_measured)

        local m_k = cos_beta_k * (estimatedPos.x - p0.x) + sin_beta_k * (estimatedPos.z - p0.z)

        local m_min = 100
        local m_sign = (m_k >= 0) and 1 or -1
        m_k = m_sign * l_math.sqrt(m_k * m_k + m_min * m_min)

        local H_k = matrix({
            { sin_beta_k / m_k, -cos_beta_k / m_k, 0, 0 }
        })

        local cos_z, sin_z = l_math.cos(beta_measured), l_math.sin(beta_measured)
        local z_pseudo = sin_z * p0.x - cos_z * p0.z
        local z_k = matrix({ { z_pseudo / m_k } })

        local R_k = matrix({ { sigma_r * sigma_r } })
        local S_k = H_k * P_k * H_k:transpose() + R_k

        local K_k = P_k * H_k:transpose() * S_k:invert()

        local y_k = z_k - H_k * x_hat

        self.state = x_hat + (K_k * y_k)
        self.P = (matrix(4, "I") - K_k * H_k) * P_k

        self.state[3][1] = HoundUtils.Mapping.clamp(self.state[3][1], -30.0, 30.0)
        self.state[4][1] = HoundUtils.Mapping.clamp(self.state[4][1], -30.0, 30.0)

        if HOUND.KALMAN_DEBUG and HOUND.DEBUG then
            self:updateMarker()
        end
    end

    setmetatable(HOUND.Contact.Estimator.UPLKF,
        { __call = function(...) return HOUND.Contact.Estimator.UPLKF.create(...) end })
end
do
    local l_math = math
    local HoundUtils = HOUND.Utils

    HOUND.Contact.Datapoint = {}
    HOUND.Contact.Datapoint.__index = HOUND.Contact.Datapoint
    HOUND.Contact.Datapoint.DataPointId = 0

    function HOUND.Contact.Datapoint.New(platform0, p0, az0, el0, s0, t0, angularResolution, isPlatformStatic)
        local elintDatapoint = {}
        setmetatable(elintDatapoint, HOUND.Contact.Datapoint)
        elintDatapoint.platformPos = p0
        elintDatapoint.az = az0
        elintDatapoint.el = el0
        elintDatapoint.signalStrength = tonumber(s0) or 0
        elintDatapoint.t = tonumber(t0)
        elintDatapoint.platformId = tonumber(platform0:getID())
        elintDatapoint.platformName = platform0:getName()
        elintDatapoint.platformStatic = isPlatformStatic or false
        elintDatapoint.platformPrecision = angularResolution or l_math.rad(HOUND.MAX_ANGULAR_RES_DEG)
        elintDatapoint.kalman = nil
        elintDatapoint.processed = false
        if elintDatapoint.platformStatic then
            elintDatapoint.kalman = HOUND.Contact.Estimator.Kalman.AzFilter(elintDatapoint.platformPrecision)
            elintDatapoint:update(elintDatapoint.az)
        end
        return elintDatapoint
    end

    function HOUND.Contact.Datapoint.isStatic(self)
        return self.platformStatic
    end

    function HOUND.Contact.Datapoint.getAge(self)
        return HoundUtils.absTimeDelta(self.t)
    end

    function HOUND.Contact.Datapoint.getPos(self)
        if self.kalman then
            return self.kalman:getValue().pos or nil
        end
        if not self.az and not self.el then return end
        self.pos = HoundUtils.Geo.getProjectedIP(self.platformPos, self.az, self.el)
        if not HountUtils.Dcs.isPoint(self.pos) then
            self.pos = HoundUtils.Geo.getProjectedIP(self.platformPos, self.az, (self.el - (self.platformPrecision/2)))
        end
        return self.pos
    end
    function HOUND.Contact.Datapoint.update(self,newAz,predictedAz,processNoise)
        if not self.platformPrecision and not self.platformStatic then return end
        self.kalman:update(newAz,nil,processNoise)
        self.az = self.kalman:get()
        return self.az
    end
end
do

    local l_math = math
    local l_mist = HOUND.Mist
    local PI_2 = l_math.pi*2
    local HoundUtils = HOUND.Utils

    HOUND.Contact.Emitter = {}
    HOUND.Contact.Emitter = HOUND.inheritsFrom(HOUND.Contact.Base)

    function HOUND.Contact.Emitter:New(DcsObject,HoundCoalition,ContactId)
        if not DcsObject or type(DcsObject) ~= "table" or not DcsObject.getName or not HoundCoalition then
            HOUND.Logger.warn("failed to create HOUND.Contact instance")
            return
        end
        local instance = self:superClass():New(DcsObject,HoundCoalition)
        setmetatable(instance, HOUND.Contact.Emitter)
        self.__index = self

        instance.uid = ContactId or tonumber(DcsObject:getID())
        instance.DcsTypeName = DcsObject:getTypeName()
        instance.DcsGroupName = Group.getName(DcsObject:getGroup())
        instance.DcsObjectName = DcsObject:getName()
        instance.DcsObjectAlive = true
        instance.typeName = DcsObject:getTypeName()
        instance.isEWR = false
        instance.typeAssigned = {"Unknown"}

        instance.isPrimary = false
        instance.radarRoles = {HOUND.DB.RadarType.SEARCH}

        local _,contactUnitCategory = DcsObject:getCategory()
        if contactUnitCategory and contactUnitCategory == Unit.Category.SHIP then
            instance.band = {
                [false] = HOUND.DB.Bands.E,
                [true] = HOUND.DB.Bands.E,
            }
            instance.typeAssigned = {"Naval"}
            instance.radarRoles = {HOUND.DB.RadarType.NAVAL}
        end

        local contactData = HOUND.DB.getRadarData(instance.DcsTypeName)
        if contactData  then
            instance.typeName =  contactData.Name
            instance.isEWR = contactData.isEWR
            instance.typeAssigned = contactData.Assigned
            instance.band = contactData.Band
            instance.isPrimary = contactData.isPrimary
            instance.radarRoles = contactData.Role
            instance.frequency = contactData.Freqency
        else
            instance.band = {
                [false] = HOUND.DB.Bands.E,
                [true] = HOUND.DB.Bands.F,
            }
            instance.frequency = HOUND.DB.getEmitterFrequencies(instance.band)
        end

        instance.uncertenty_data = nil
        instance.maxWeaponsRange = HoundUtils.Dcs.getSamMaxRange(DcsObject)
        instance.detectionRange = HoundUtils.Dcs.getRadarDetectionRange(DcsObject)
        instance._dataPoints = {}
        instance.detected_by = {}
        instance.state = HOUND.EVENTS.RADAR_NEW
        instance.preBriefed = false
        instance.unitAlive = true
        instance.Kalman = nil
        return instance
    end

    function HOUND.Contact.Emitter:destroy()
        self:removeMarkers()
        self.state=HOUND.EVENTS.RADAR_DESTROYED
        self:queueEvent(HOUND.EVENTS.RADAR_DESTROYED)
    end

    function HOUND.Contact.Emitter:getName()
        return self:getType() .. " " .. self:getId()
    end

    function HOUND.Contact.Emitter:getType()
        return self.typeName
    end

    function HOUND.Contact.Emitter:getId()
        return self.uid%100
    end

    function HOUND.Contact.Emitter:getTrackId()
        local trackType = 'E'
        if self:isAccurate() then
            trackType = 'I'
        end
        return string.format("%s-%d",trackType,self.uid)
    end

    function HOUND.Contact.Emitter:getPos()
        return self.pos.p
    end

    function HOUND.Contact.Emitter:getWavelenght(isTracking)
        isTracking = isTracking or false
        return self.frequency[isTracking]
    end

    function HOUND.Contact.Emitter:getElev()
        if not self:hasPos() then return 0 end
        local step = 50
        if self:isAccurate() then
            step = 1
        end
        return HoundUtils.getRoundedElevationFt(self.pos.elev,step)
    end

    function HOUND.Contact.Emitter:getLife()
        if self:isAlive() and (not HoundUtils.Dcs.isUnit(self.DcsObject)) then
            HOUND.Logger.error("something is wrong with the object for " .. self.DcsObjectName)
            self:setDead()
        end
        if self.DcsObject and type(self.DcsObject) == "table" and self.DcsObject:isExist() then
            return self.DcsObject:getLife(),(self.DcsObject:getLife()/self.DcsObject:getLife0())
        end
        return 0
    end

    function HOUND.Contact.Emitter:isAlive()
        return self.DcsObjectAlive
    end

    function HOUND.Contact.Emitter:setDead()
        self.DcsObjectAlive = false
        self:updateDeadDcsObject()
    end

    function HOUND.Contact.Emitter:updateDeadDcsObject()
        self.DcsObject = Unit.getByName(self.DcsObjectName) or StaticObject.getByName(self.DcsObjectName)
        if not self.DcsObject then
            self.DcsObject = self.DcsObjectName
        end
    end

    function HOUND.Contact.Emitter:CleanTimedout()
        if self:isTimedout() then
            self._dataPoints = {}
            self.state = HOUND.EVENTS.RADAR_ASLEEP
        end
        return self.state
    end

    function HOUND.Contact.Emitter:countPlatforms(skipStatic)
        local count = 0
        if HOUND.Length(self._dataPoints) == 0 then return count end
        for _,platformDataPoints in pairs(self._dataPoints) do
            if not platformDataPoints[1].staticPlatform or (not skipStatic and platformDataPoints[1].staticPlatform) then
                count = count + 1
            end
        end
        return count
    end

    function HOUND.Contact.Emitter:countDatapoints()
        local count = 0
        if HOUND.Length(self._dataPoints) == 0 then return count end
        for _,platformDataPoints in pairs(self._dataPoints) do
            count = count + HOUND.Length(platformDataPoints)
        end
        return count
    end

    function HOUND.Contact.Emitter:KalmanPredict(timestamp)
        timestamp = timestamp or timer.getAbsTime()
        if HOUND.ENABLE_KALMAN and self.Kalman then
            self.Kalman:predict(timestamp)
        end

    end
    function HOUND.Contact.Emitter:AddPoint(datapoint)
        if HOUND.ENABLE_KALMAN and not self.Kalman and HoundUtils.Dcs.isPoint(self.pos.p) then
            if self.uncertenty_data.r < 15000 then
                self.Kalman = HOUND.Contact.Estimator.UPLKF(self.pos.p,{x=0,z=0},self.last_seen,self.uncertenty_data.major)
            end
        end
        self.last_seen = datapoint.t
        if HOUND.ENABLE_KALMAN and self.Kalman then
            self.Kalman:update(datapoint.platformPos,datapoint.az,datapoint.t,datapoint.platformPrecision)
        end
        self._platforms = self._platforms or {}
        self._platforms[datapoint.platformName] = datapoint.t
        if HOUND.ENABLE_KALMAN and self.Kalman then
            return
        end
        if HOUND.Length(self._dataPoints[datapoint.platformId]) == 0 then
            self._dataPoints[datapoint.platformId] = {}
        end

        if datapoint.platformStatic then
            if HOUND.Length(self._dataPoints[datapoint.platformId]) == 0 then
                self._dataPoints[datapoint.platformId] = {datapoint}
                return
            end
            local predicted = {}
            if HoundUtils.Dcs.isPoint(self.pos.p) then
                predicted.az,predicted.el = HoundUtils.Elint.getAzimuth( datapoint.platformPos , self.pos.p, 0.0 )
                if type(self.uncertenty_data) == "table" and self.uncertenty_data.minor and self.uncertenty_data.major and self.uncertenty_data.az then
                    predicted.err = HoundUtils.Polygon.azMinMax(HOUND.Contact.Emitter.calculatePoly(self.uncertenty_data,8,self.pos.p),datapoint.platformPos)
                end
            end
            self._dataPoints[datapoint.platformId][1]:update(datapoint.az,predicted.az,predicted.err)
            return
        end

        if HOUND.Length(self._dataPoints[datapoint.platformId]) < 2 then
            table.insert(self._dataPoints[datapoint.platformId], 1, datapoint)
            return
        else
            local DeltaT = self._dataPoints[datapoint.platformId][2]:getAge() - datapoint:getAge()
            if  DeltaT >= HOUND.DATAPOINTS_INTERVAL then
                table.insert(self._dataPoints[datapoint.platformId], 1, datapoint)
            else
                self._dataPoints[datapoint.platformId][1] = datapoint
            end
        end

        for i=HOUND.Length(self._dataPoints[datapoint.platformId]),1,-1 do
            if self._dataPoints[datapoint.platformId][i]:getAge() > HOUND.CONTACT_TIMEOUT then
                table.remove(self._dataPoints[datapoint.platformId])
            else
                i=1
            end
        end

        if self:countPlatforms(true) > 0 then
            local pointsPerPlatform = l_math.ceil(HOUND.DATAPOINTS_NUM/self:countPlatforms(true))
            while HOUND.Length(self._dataPoints[datapoint.platformId]) > pointsPerPlatform do
                table.remove(self._dataPoints[datapoint.platformId])
            end
        end
    end

    function HOUND.Contact.Emitter.triangulatePoints(earlyPoint, latePoint)
        local p1 = earlyPoint.platformPos
        local p2 = latePoint.platformPos

        local m1 = l_math.tan(earlyPoint.az)
        local m2 = l_math.tan(latePoint.az)

        local b1 = -m1 * p1.x + p1.z
        local b2 = -m2 * p2.x + p2.z

        local Easting = (b2 - b1) / (m1 - m2)
        local Northing = m1 * Easting + b1

        local pos = {}

        pos.x = Easting
        pos.z = Northing
        pos.y = land.getHeight({x=pos.x,y=pos.z})

        local d1 = HoundUtils.Geo.get2DDistance(p1,pos)
        local d2 = HoundUtils.Geo.get2DDistance(p2,pos)
        local var1 = l_math.pow(((d1 / HOUND.REF_DIST) * earlyPoint.platformPrecision),2)
        local var2 = l_math.pow(((d2 / HOUND.REF_DIST) * latePoint.platformPrecision),2)
        local score = 1 / l_math.max((var1+var2),1e-10)
        local angleScore = l_math.sin(HoundUtils.angleDeltaRad(earlyPoint.az,latePoint.az))
        pos.score = score * angleScore
        return pos
    end

    function HOUND.Contact.Emitter.calculateEllipse(estimatedPositions,refPos)
        local percentile = HOUND.ELLIPSE_PERCENTILE
        local RelativeToPos = HoundUtils.Cluster.getDeltaSubsetPercent(estimatedPositions,refPos,percentile,true)

        local min = {}
        min.x = 99999
        min.y = 99999

        local max = {}
        max.x = -99999
        max.y = -99999

        Theta = HoundUtils.PointClusterTilt(RelativeToPos)

        local sinTheta = l_math.sin(-Theta)
        local cosTheta = l_math.cos(-Theta)

        for k,pos in ipairs(RelativeToPos) do
            local newPos = {}
            newPos.x = pos.x*cosTheta - pos.z*sinTheta
            newPos.z = pos.x*sinTheta + pos.z*cosTheta
            newPos.y = pos.y

            min.x = l_math.min(min.x,newPos.x)
            max.x = l_math.max(max.x,newPos.x)
            min.y = l_math.min(min.y,newPos.z)
            max.y = l_math.max(max.y,newPos.z)

            RelativeToPos[k] = newPos
        end

        local a = l_mist.utils.round(l_math.abs(min.x)+l_math.abs(max.x))
        local b = l_mist.utils.round(l_math.abs(min.y)+l_math.abs(max.y))

        local uncertenty_data = {}
        uncertenty_data.major = l_math.max(a,b)
        uncertenty_data.minor = l_math.min(a,b)
        uncertenty_data.theta = (Theta + PI_2) % PI_2
        uncertenty_data.az = l_mist.utils.round(l_math.deg(uncertenty_data.theta))
        uncertenty_data.r  = (a+b)/4

        return uncertenty_data
    end

    function HOUND.Contact.Emitter:calculateExtrasPosData(pos)
        if HoundUtils.Dcs.isPoint(pos.p) then
            local bullsPos = coalition.getMainRefPoint(self._platformCoalition)
            pos.LL = {}
            pos.LL.lat, pos.LL.lon = coord.LOtoLL(pos.p)
            pos.elev = pos.p.y
            pos.grid  = coord.LLtoMGRS(pos.LL.lat, pos.LL.lon)
            pos.be = HoundUtils.getBR(bullsPos,pos.p)
        end
        return pos
    end

    function HOUND.Contact.Emitter:processIntersection(targetTable,point1,point2)
        if point1.platformPos.x == point2.platformPos.x and point1.platformPos.z == point2.platformPos.z then return end
        local err = (point1.platformPrecision + point2.platformPrecision)/2
        if HoundUtils.angleDeltaRad(point1.az,point2.az) < err then return end
        local intersection = self.triangulatePoints(point1,point2)
        if not HoundUtils.Dcs.isPoint(intersection) then return end
        table.insert(targetTable,intersection)
    end

    function HOUND.Contact.Emitter:processData()
        if self:getPreBriefed() then
            if type(self.DcsObject) == "table" and type(self.DcsObject.isExist) == "function" and self.DcsObject:isExist()
                then
                    local unitPos = self.DcsObject:getPosition()
                    if HoundUtils.Geo.get2DDistance(unitPos.p,self.pos.p) < 0.25 then return end
                    if self:isActive() then
                        HOUND.Logger.debug(self:getName().. " is active and moved.. not longer PB")
                        self:setPreBriefed(false)
                    end
                else
                    self.state = HOUND.EVENTS.NO_CHANGE
                    return
            end
        end

        if not self:isRecent() and self.state ~= HOUND.EVENTS.RADAR_NEW then
            return self.state
        end

        if HOUND.ENABLE_KALMAN and self.Kalman then
            local newContact = (self.state == HOUND.EVENTS.RADAR_NEW)

            self.pos.p = self.Kalman:getEstimatedPos()
            self.uncertenty_data = self.Kalman:getUncertainty()
            self.uncertenty_data.az = l_mist.utils.round(l_math.deg((self.uncertenty_data.theta+l_mist.getNorthCorrection(self.pos.p)+PI_2)%PI_2))

            self:calculateExtrasPosData(self.pos)

            if self.state == HOUND.EVENTS.RADAR_ASLEEP then
                self.state = HOUND.EVENTS.SITE_ALIVE
            else
                self.state = HOUND.EVENTS.RADAR_UPDATED
            end

            if self._platforms then
                local detected_by = {}
                local currentTime = timer.getAbsTime()
                for name, lastSeen in pairs(self._platforms) do
                    if (currentTime - lastSeen) <= HOUND.CONTACT_TIMEOUT then
                        table.insert(detected_by, name)
                    end
                end
                self.detected_by = detected_by
            end

            if newContact and HoundUtils.Dcs.isPoint(self.pos.p) ~= nil and self.isEWR == false then
                self.state = HOUND.EVENTS.RADAR_DETECTED
            end

            self:queueEvent(self.state)
            return self.state
        end

        local newContact = (self.state == HOUND.EVENTS.RADAR_NEW)
        local mobileDataPoints = {}
        local staticDataPoints = {}
        local estimatePositions = {}
        local staticPlatformsOnly = true
        local AllDataPoints = {}

        for _,platformDatapoints in pairs(self._dataPoints) do
            if HOUND.Length(platformDatapoints) > 0 then
                for _,datapoint in pairs(platformDatapoints) do
                    if datapoint:isStatic() then
                        table.insert(staticDataPoints,datapoint)
                    else
                        staticPlatformsOnly = false
                        table.insert(mobileDataPoints,datapoint)
                    end
                end
            end
        end
        local numMobilepoints = HOUND.Length(mobileDataPoints)
        local numStaticPoints = HOUND.Length(staticDataPoints)
        table.sort(mobileDataPoints, function(a,b) return a.signalStrength < b.signalStrength end)
        table.sort(staticDataPoints, function(a,b) return a.signalStrength < b.signalStrength end)

        if numMobilepoints+numStaticPoints < 2 and HOUND.Length(estimatePositions) == 0 then return end
        if numStaticPoints > 1 then
            for i=1,numStaticPoints-1 do
                for j=i+1,numStaticPoints do
                    self:processIntersection(estimatePositions,staticDataPoints[i],staticDataPoints[j])
                end
            end
        end

        if numStaticPoints > 0  and numMobilepoints > 0 then
            for _,staticDataPoint in ipairs(staticDataPoints) do
                for _,mobileDataPoint in ipairs(mobileDataPoints) do
                    self:processIntersection(estimatePositions,staticDataPoint,mobileDataPoint)
                end
            end
         end

        if numMobilepoints > 1 then
            for i=1,numMobilepoints-1 do
                for j=i+1,numMobilepoints do
                    if mobileDataPoints[i].platformPos ~= mobileDataPoints[j].platformPos then
                        self:processIntersection(estimatePositions,mobileDataPoints[i],mobileDataPoints[j])
                    end
                end
                mobileDataPoints[i].processed = true
            end
        end

        if HOUND.Length(estimatePositions) > 2 or (HOUND.Length(estimatePositions) > 0 and staticPlatformsOnly) then
            self.pos.p = HoundUtils.Cluster.WeightedCentroid(estimatePositions)
            self.uncertenty_data = self.calculateEllipse(estimatePositions,self.pos.p)

            self.uncertenty_data.az = l_mist.utils.round(l_math.deg((self.uncertenty_data.theta+l_mist.getNorthCorrection(self.pos.p)+PI_2)%PI_2))

            self:calculateExtrasPosData(self.pos)

            if self.state == HOUND.EVENTS.RADAR_ASLEEP then
                self.state = HOUND.EVENTS.SITE_ALIVE
            else
                self.state = HOUND.EVENTS.RADAR_UPDATED
            end

            if self._platforms then
                local detected_by = {}
                local currentTime = timer.getAbsTime()
                for name, lastSeen in pairs(self._platforms) do
                    if (currentTime - lastSeen) <= HOUND.CONTACT_TIMEOUT then
                        table.insert(detected_by, name)
                    end
                end
                self.detected_by = detected_by
            end
        end

        if newContact and HoundUtils.Dcs.isPoint(self.pos.p) ~= nil and self.isEWR == false then
            self.state = HOUND.EVENTS.RADAR_DETECTED
            self:calculateExtrasPosData(self.pos)
        end
        self:queueEvent(self.state)
        return self.state
    end

    function HOUND.Contact.Emitter.calculatePoly(uncertenty_data,numPoints,refPos)
        local polygonPoints = {}
        if type(uncertenty_data) ~= "table" or not uncertenty_data.major or not uncertenty_data.minor or not uncertenty_data.az then
            return polygonPoints
        end
        if type(numPoints) ~= "number" then
            numPoints = 8
        end
        if not HoundUtils.Dcs.isPoint(refPos) then
            refPos = {x=0,y=0,z=0}
        end
        local angleStep = PI_2/numPoints
        local theta = l_math.rad(uncertenty_data.az) - HoundUtils.getMagVar(refPos)
        local cos_theta,sin_theta = l_math.cos(theta),l_math.sin(theta)
        for i = 1, numPoints do
            local pointAngle = PI_2 - (i * angleStep)
            local point = {}
            point.x = uncertenty_data.major/2 * l_math.cos(pointAngle)
            point.z = uncertenty_data.minor/2 * l_math.sin(pointAngle)
            local x = point.x * cos_theta - point.z * sin_theta
            local z = point.x * sin_theta + point.z * cos_theta
            point.x = x + refPos.x
            point.z = z + refPos.z
            local mgrs = coord.LLtoMGRS(coord.LOtoLL( point ))
            if type(mgrs) == "table" and type(mgrs.Easting) == "number" and type(mgrs.Northing ) == "number" then
                table.insert(polygonPoints, point)
            end
        end
        HoundUtils.Geo.setHeight(polygonPoints)

        return polygonPoints
    end

    function HOUND.Contact.Emitter:drawAreaMarker(numPoints)
        if numPoints == nil then numPoints = 1 end
        if numPoints ~= 1 and numPoints ~= 4 and numPoints ~=8 and numPoints ~= 16 then
            HOUND.Logger.error("DCS limitation, only 1,4,8 or 16 points are allowed")
            numPoints = 1
            end

        local alpha = HoundUtils.Mapping.linear(l_math.floor(HoundUtils.absTimeDelta(self.last_seen)),0,HOUND.CONTACT_TIMEOUT,HOUND.MARKER_MAX_ALPHA,HOUND.MARKER_MIN_ALPHA,true)
        local fillColor = {0,0,0,alpha}
        local lineColor = {0,0,0,HOUND.MARKER_LINE_OPACITY}
        local lineType = 2
        if (HoundUtils.absTimeDelta(self.last_seen) < 30) then
            lineType = 1
        end
        if self._platformCoalition == coalition.side.BLUE then
            fillColor[1] = 1
            lineColor[1] = 1
        elseif self._platformCoalition == coalition.side.RED then
            fillColor[3] = 1
            lineColor[3] = 1
        end

        local markArgs = {
            fillColor = fillColor,
            lineColor = lineColor,
            coalition = self._platformCoalition,
            lineType = lineType
        }
        if numPoints == 1 then
            markArgs.pos = {
                p = self.pos.p,
                r = self.uncertenty_data.r
            }
        else
            markArgs.pos = HOUND.Contact.Emitter.calculatePoly(self.uncertenty_data,numPoints,self.pos.p)
        end
        return self._markpoints.area:update(markArgs)
    end

    function HOUND.Contact.Emitter:updateMarker(MarkerType)
        local MarkerType = MarkerType or HOUND.MARKER.POINT
        if not self:hasPos() or self.uncertenty_data == nil or not self:isRecent() then return end
        if self:isAccurate() and self._markpoints.pos:isDrawn() then return end
        local markerArgs = {
            text = self.typeName .. " " .. (self.uid%100),
            pos = self.pos.p,
            coalition = self._platformCoalition,
            useLegacyMarker = HOUND.USE_LEGACY_MARKERS
        }
        if not self:isAccurate() and HOUND.USE_LEGACY_MARKERS then
            markerArgs.text = markerArgs.text .. " (" .. self.uncertenty_data.major .. "/" .. self.uncertenty_data.minor .. "@" .. self.uncertenty_data.az .. ")"
        end
        if MarkerType >= HOUND.MARKER.POINT then
            self._markpoints.pos:update(markerArgs)
        end

        if  MarkerType < HOUND.MARKER.POINT or self:isAccurate() then
                self._markpoints.area:remove()
                if MarkerType < HOUND.MARKER.POINT then
                    self._markpoints.pos:remove()
                end
            return
        end

        if MarkerType == HOUND.MARKER.CIRCLE then
            self:drawAreaMarker()
        end

        if MarkerType == HOUND.MARKER.DIAMOND then
            self:drawAreaMarker(4)
        end

        if MarkerType == HOUND.MARKER.OCTAGON then
            self:drawAreaMarker(8)
        end

        if MarkerType == HOUND.MARKER.POLYGON then
            self:drawAreaMarker(16)
        end
    end

    function HOUND.Contact.Emitter:useUnitPos(unitPosMarker)
        if not self.DcsObject:isExist() then
            HOUND.Logger.info("PB failed - unit does not exist")
            return
        end
        self.state = HOUND.EVENTS.RADAR_DETECTED
        if type(self.pos.p) == "table" then
            self.state = HOUND.EVENTS.RADAR_UPDATED
        end
        local unitPos = self.DcsObject:getPosition()
        self:setPreBriefed(true)

        self.pos.p = l_mist.utils.deepCopy(unitPos.p)
        self:calculateExtrasPosData(self.pos)

        self.uncertenty_data = {}
        self.uncertenty_data.major = 0.1
        self.uncertenty_data.minor = 0.1
        self.uncertenty_data.az = 0
        self.uncertenty_data.r  = 0.1

        table.insert(self.detected_by,"External")
        self:updateMarker(unitPosMarker)
        return self.state
    end

    function HOUND.Contact.Emitter:export()
        local contact = {}
        contact.typeName = self.typeName
        contact.uid = self.uid % 100
        contact.DcsObjectName = self.DcsObject:getName()
        if self.pos.p ~= nil and self.uncertenty_data ~= nil then
            contact.pos = self.pos.p
            contact.LL = self.pos.LL

            contact.accuracy = HoundUtils.TTS.getVerbalConfidenceLevel( self.uncertenty_data.r )
            contact.uncertenty = {
                major = self.uncertenty_data.major,
                minor = self.uncertenty_data.minor,
                heading = self.uncertenty_data.az
            }
        end
        contact.maxWeaponsRange = self.maxWeaponsRange
        contact.last_seen = self.last_seen
        contact.detected_by = self.detected_by
        return l_mist.utils.deepCopy(contact)
    end
end
do
    local l_math = math
    local HoundUtils = HOUND.Utils

    function HOUND.Contact.Emitter:getTextData(utmZone,MGRSdigits)
        if self.pos.p == nil then return end
        local GridPos = ""
        if utmZone then
            GridPos = GridPos .. self.pos.grid.UTMZone .. " "
        end
        GridPos = GridPos .. self.pos.grid.MGRSDigraph
        local BE = self.pos.be.brStr .. "/" .. self.pos.be.rng
        if MGRSdigits == nil then
            return GridPos,BE
        end
        local E = l_math.floor(self.pos.grid.Easting/(10^l_math.min(5,l_math.max(1,5-MGRSdigits))))
        local N = l_math.floor(self.pos.grid.Northing/(10^l_math.min(5,l_math.max(1,5-MGRSdigits))))
        GridPos = GridPos .. " " .. E .. " " .. N

        return GridPos,BE
    end

    function HOUND.Contact.Emitter:getTtsData(utmZone,MGRSdigits)
        if self.pos.p == nil then return end
        local phoneticGridPos = ""
        if utmZone then
            phoneticGridPos =  phoneticGridPos .. HoundUtils.TTS.toPhonetic(self.pos.grid.UTMZone) .. " "
        end

        phoneticGridPos =  phoneticGridPos ..  HoundUtils.TTS.toPhonetic(self.pos.grid.MGRSDigraph)
        local phoneticBulls = HoundUtils.TTS.toPhonetic(self.pos.be.brStr)
                                .. "  " .. self.pos.be.rng
        if MGRSdigits==nil then
            return phoneticGridPos,phoneticBulls
        end
        local E = l_math.floor(self.pos.grid.Easting/(10^l_math.min(5,l_math.max(1,5-MGRSdigits))))
        local N = l_math.floor(self.pos.grid.Northing/(10^l_math.min(5,l_math.max(1,5-MGRSdigits))))
        phoneticGridPos = phoneticGridPos .. " " .. HoundUtils.TTS.toPhonetic(E) .. "   " .. HoundUtils.TTS.toPhonetic(N)

        return phoneticGridPos,phoneticBulls
    end

    function HOUND.Contact.Emitter:generateTtsBrief(NATO)
        if self.pos.p == nil or self.uncertenty_data == nil then return end
        local phoneticGridPos,phoneticBulls = self:getTtsData(false,1)
        local reportedName = self:getName()
        if NATO then
            reportedName = self:getDesignation(NATO)
        end
        local str = reportedName
        if self:isAccurate() then
            str = str .. ", reported"
        else
            str = str .. ", " .. HoundUtils.TTS.getVerbalContactAge(self.last_seen,true,NATO)
        end
        if NATO then
            str = str .. " bullseye " .. phoneticBulls
        else
            str = str .. " at " .. phoneticGridPos
        end
        if not self:isAccurate() then
            str = str .. ", accuracy " .. HoundUtils.TTS.getVerbalConfidenceLevel( self.uncertenty_data.r )
        end
        str = str .. "."
        return str
    end

    function HOUND.Contact.Emitter:generateTtsReport(useDMM,preferMGRS,refPos)
        if self.pos.p == nil then return end
        useDMM = useDMM or false
        preferMGRS = preferMGRS or false
        local MGRSPrecision = HOUND.MGRS_PRECISION
        if preferMGRS then
            MGRSPrecision = 5;
        end
        local BR = nil
        if refPos ~= nil and refPos.x ~= nil and refPos.z ~= nil then
            BR = HoundUtils.getBR(self.pos.p,refPos)
        end
        local phoneticGridPos,phoneticBulls = self:getTtsData(true,MGRSPrecision)
        local msg =  self:getName()
        if self:isAccurate()
            then
                msg = msg .. ", reported"
            else
               msg = msg .. ", " .. HoundUtils.TTS.getVerbalContactAge(self.last_seen,true)
        end
        if BR ~= nil
            then
                msg = msg .. " from you " .. HoundUtils.TTS.toPhonetic(BR.brStr) .. " for " .. BR.rng
            else
                msg = msg .." at bullseye " .. phoneticBulls
        end
        local LLstr = HoundUtils.TTS.getVerbalLL(self.pos.LL.lat,self.pos.LL.lon,useDMM)

        local primaryPos = LLstr
        if preferMGRS then
            primaryPos = phoneticGridPos
        end

        msg = msg .. ", accuracy " .. HoundUtils.TTS.getVerbalConfidenceLevel( self.uncertenty_data.r )
        msg = msg .. ", position " .. primaryPos
        msg = msg .. ", I say again " .. primaryPos
        if not preferMGRS then
            msg = msg .. ", MGRS " .. phoneticGridPos
        end
        msg = msg .. ", elevation  " .. self:getElev() .. " feet MSL"

        if HOUND.EXTENDED_INFO then
            if self:isAccurate()
                then
                    msg = msg .. ", Reported " .. HoundUtils.TTS.getVerbalContactAge(self.first_seen) .. " ago"
                else
                    msg = msg .. ", ellipse " ..  HoundUtils.TTS.simplfyDistance(self.uncertenty_data.major) .. " by " ..  HoundUtils.TTS.simplfyDistance(self.uncertenty_data.minor) .. ", aligned bearing " .. HoundUtils.TTS.toPhonetic(string.format("%03d",self.uncertenty_data.az))
                    msg = msg .. ", Tracked for " .. HoundUtils.TTS.getVerbalContactAge(self.first_seen) .. ", last seen " .. HoundUtils.TTS.getVerbalContactAge(self.last_seen) .. " ago"
                end
        end
        msg = msg .. ". " .. HoundUtils.getControllerResponse()
        return msg
    end

    function HOUND.Contact.Emitter:generateTextReport(useDMM,refPos)
        if self.pos.p == nil then return end
        useDMM = useDMM or false

        local GridPos,BePos = self:getTextData(true,HOUND.MGRS_PRECISION)
        local BR = nil
        if refPos ~= nil and refPos.x ~= nil and refPos.z ~= nil then
            BR = HoundUtils.getBR(self.pos.p,refPos)
        end
        local msg =  self:getName()
        if self:isAccurate()
            then
                msg = msg .." (Reported)\n"
            else
                msg = msg .." (" .. HoundUtils.TTS.getVerbalContactAge(self.last_seen,true).. ")\n"
        end
        msg = msg .. "Accuracy: " .. HoundUtils.TTS.getVerbalConfidenceLevel( self.uncertenty_data.r ) .. "\n"
        msg = msg .. "BE: " .. BePos .. "\n" -- .. " (grid ".. GridPos ..")\n"
        if BR ~= nil then
            msg = msg .. "BR: " .. BR.brStr .. " for " .. BR.rng
        end
        msg = msg .. "LL: " .. HoundUtils.Text.getLL(self.pos.LL.lat,self.pos.LL.lon,useDMM).."\n"
        msg = msg .. "MGRS: " .. GridPos .. "\n"
        msg = msg .. "Elev: " .. self:getElev() .. "ft"
        if HOUND.EXTENDED_INFO then
            if self:isAccurate() then
                msg = msg .. "\nReported " .. HoundUtils.TTS.getVerbalContactAge(self.first_seen) .. " ago. "
            else
                msg = msg .. "\nEllipse: " ..  self.uncertenty_data.major .. " by " ..  self.uncertenty_data.minor .. " aligned bearing " .. string.format("%03d",self.uncertenty_data.az) .. "\n"
                msg = msg .. "Tracked for: " .. HoundUtils.TTS.getVerbalContactAge(self.first_seen) .. " Last Contact: " ..  HoundUtils.TTS.getVerbalContactAge(self.last_seen) .. " ago. "
            end
        end
        return msg
    end

    function HOUND.Contact.Emitter:getRadioItemText()
        if not self:hasPos() then return self:getName() end
        local GridPos,BePos = self:getTextData(true,1)
        BePos = BePos:gsub(" for ","/")
        return self:getName() .. " - BE: " .. BePos .. " (".. GridPos ..")"
    end

    function HOUND.Contact.Emitter:generatePopUpReport(isTTS,sectorName)
        local msg = self:getName()
        if self:isAccurate() then
            msg = msg .. " has been reported"
        else
            msg = msg .. " is now Alive"
        end

        if sectorName then
            msg = msg .. " in " .. sectorName
        else
            if self:hasPos() then
                local GridPos,BePos
                if isTTS then
                    GridPos,BePos = self:getTtsData(true,1)
                    msg = msg .. ", bullseye " .. BePos .. ", grid ".. GridPos
                else
                    GridPos,BePos = self:getTextData(true,1)
                    msg = msg .. " BE: " .. BePos .. " (grid ".. GridPos ..")"
                end
            end
        end
        return msg .. "."
    end

    function HOUND.Contact.Emitter:generateDeathReport(isTTS,sectorName)
        local msg = self:getName() .. " has been destroyed"
        if sectorName then
            msg = msg .. " in " .. sectorName
        else
            if self:hasPos() then
                local GridPos,BePos
                if isTTS then
                    GridPos,BePos = self:getTtsData(true,1)
                    msg = msg .. ", bullseye " .. BePos .. ", grid ".. GridPos
                else
                    GridPos,BePos = self:getTextData(true,1)
                    msg = msg .. " BE: " .. BePos .. " (grid ".. GridPos ..")"
                end
            end
        end
        return msg .. "."
    end

    function HOUND.Contact.Emitter:generateIntelBrief()
        local msg = ""
        if self:hasPos() then
            local GridPos,BePos = self:getTextData(true,HOUND.MGRS_PRECISION)
            msg = {
                self:getTrackId(),self:getType(),
                HoundUtils.TTS.getVerbalContactAge(self.last_seen,true,true),
                BePos,string.format("%02.6f",self.pos.LL.lat),string.format("%03.6f",self.pos.LL.lon), GridPos,
                HoundUtils.TTS.getVerbalConfidenceLevel( self.uncertenty_data.r ),
                HoundUtils.Text.getTime(self.last_seen),self.DcsTypeName,self.DcsObjectName
            }
            msg = table.concat(msg,",")
        end
        return msg
    end
end
do
    HOUND.Contact.Site = {}
    HOUND.Contact.Site = HOUND.inheritsFrom(HOUND.Contact.Base)

    local l_math = math
    local l_mist = HOUND.Mist
    local HoundUtils = HOUND.Utils

    function HOUND.Contact.Site:New(HoundContact,HoundCoalition,SiteId)
        if not HoundContact or type(HoundContact) ~= "table" or not HoundContact.getDcsGroupName or not HoundCoalition then
            HOUND.Logger.warn("failed to create HOUND.Contact.Site instance")
            return
        end
        local instance = self:superClass():New(HoundContact:getDcsObject(),HoundCoalition)
        setmetatable(instance, HOUND.Contact.Site)
        self.__index = self
        instance.DcsObject = HoundContact:getDcsObject():getGroup()
        instance.gid = SiteId or tonumber(instance.DcsObject:getId())
        instance.DcsGroupName = instance.DcsObject:getName()
        instance.DcsObjectName = instance.DcsObject:getName()
        instance.typeAssigned = HoundContact.typeAssigned

        instance.emitters = { HoundContact }
        instance.primaryEmitter = HoundContact
        instance.last_seen = HoundContact:getLastSeen()
        instance.first_seen = HoundContact.first_seen
        instance.last_launch_notify = 0
        instance.maxWeaponsRange = HoundContact:getMaxWeaponsRange()
        instance.detectionRange = HoundContact:getRadarDetectionRange()
        instance.isEWR = HoundContact.isEWR
        instance.state = HOUND.EVENTS.SITE_NEW
        instance.preBriefed = HoundContact:isAccurate()
        instance.DcsRadarUnits = HoundUtils.Dcs.getRadarUnitsInGroup(instance.DcsObject)
        setmetatable(instance.emitters,{__mode="v"})
        return instance
    end

    function HOUND.Contact.Site:destroy()
        self:removeMarkers()
    end

    function HOUND.Contact.Site:getName()
        local prefix = 'T'
        if self.isEWR then
            prefix = 'S'
        end

        return self.name or string.format("%s%03d",prefix,self:getId())
    end

    function HOUND.Contact.Site:setName(requestedName)
        if type(requestedName) == "string" or type(requestedName) == "nil" then
            self.name = requestedName
        end
    end

    function HOUND.Contact.Site:getType()
        return self:getTypeAssigned()
    end

    function HOUND.Contact.Site:getId()
        return self.gid%1000
    end

    function HOUND.Contact.Site:getDcsGroupName()
        return self.DcsGroupName
    end

    function HOUND.Contact.Site:getDcsName()
        return self.DcsGroupName
    end

    function HOUND.Contact.Site:getDcsObject()
        return self.DcsObject or self.DcsGroupName
    end

    function HOUND.Contact.Site:getLastSeen()
        return HoundUtils.absTimeDelta(self.last_seen)
    end

    function HOUND.Contact.Site:getTypeAssigned()
        return table.concat(self.typeAssigned," or ")
    end

    function HOUND.Contact.Site:isActive()
        return self:getLastSeen()/16 < 1.0
    end

    function HOUND.Contact.Site:isRecent()
        return self:getLastSeen()/120 < 1.0
    end

    function HOUND.Contact.Site:isAccurate()
        return self.preBriefed
    end

    function HOUND.Contact.Site:isAlive()
        return #self.emitters > 0
    end

    function HOUND.Contact.Site:isTimedout()
        return self:getLastSeen() > HOUND.CONTACT_TIMEOUT
    end

    function HOUND.Contact.Site:getState()
        return self.state
    end

    function HOUND.Contact.Site:getPos()
        return self.pos.p or nil
    end

    function HOUND.Contact.Site:hasRadarUnits()
        if not HoundUtils.Dcs.isGroup(self.DcsObject) or self.DcsObject:getSize() == 0 then return false end
        local lastUnit = self.DcsObject:getUnit(self.DcsObject:getSize())
        return lastUnit:hasSensors(Unit.SensorType.RADAR)
    end

    function HOUND.Contact.Site:addEmitter(HoundEmitter)
        self.state = HOUND.EVENTS.NO_CHANGE
        if HoundEmitter:getDcsGroupName() == self:getDcsGroupName() and
            not HOUND.setContainsValue(self.emitters,HoundEmitter) then
                table.insert(self.emitters,HoundEmitter)
                self:selectPrimaryEmitter()
                self:updateTypeAssigned()
                self:updateSector()
                self:updateGroupRadars()
                self.state = HOUND.EVENTS.SITE_UPDATED
        end
        return self.state
    end

    function HOUND.Contact.Site:removeEmitter(HoundEmitter)
        self.state = HOUND.EVENTS.NO_CHANGE
        if HoundEmitter:getDcsGroupName() == self:getDcsGroupName() then
            for idx,emitter in ipairs(self.emitters) do
                if emitter == HoundEmitter then
                    table.remove(self.emitters,idx)
                    if #self.emitters > 0 then
                        self:selectPrimaryEmitter()
                    end
                    self:updateGroupRadars()
                    self.state = HOUND.EVENTS.SITE_UPDATED
                    break
                end
            end
        end
        return self.state
    end

    function HOUND.Contact.Site:gcEmitters()
        for idx=#self.emitters,1,-1 do
            if self.emitters[idx] == nil then
                table.remove(self.emitters,idx)
            end
        end
    end

    function HOUND.Contact.Site:updateGroupRadars()
        self.DcsRadarUnits = HoundUtils.Dcs.getRadarUnitsInGroup(self.DcsObject)
    end

    function HOUND.Contact.Site:getPrimary()
        if not self.primaryEmitter then
            self:selectPrimaryEmitter()
        end
        return self.primaryEmitter
    end

    function HOUND.Contact.Site:getEmitters()
        return self.emitters
    end

    function HOUND.Contact.Site:countEmitters()
        return #self.emitters
    end
    function HOUND.Contact.Site:sortEmitters()
        table.sort(self.emitters,HoundUtils.Sort.ContactsByPrio)
    end

    function HOUND.Contact.Site:selectPrimaryEmitter()
        self:sortEmitters()
        if self.primaryEmitter ~= self.emitters[1] then
            self.primaryEmitter = self.emitters[1]
            self.isEWR = self.primaryEmitter.isEWR
            self.state = HOUND.EVENTS.SITE_UPDATED
            return true
        end
        return false
    end

    function HOUND.Contact.Site:updateTypeAssigned()
        local type = self.primaryEmitter.typeAssigned or {}
        if HOUND.Length(type) > 1 then
            for _,emitter in ipairs(self.emitters) do
                type = HOUND.setIntersection(type,emitter.typeAssigned)
            end
        end
        if self:getTypeAssigned() ~= table.concat(type," or ") then
            self.typeAssigned = type

            if self.state ~= HOUND.EVENTS.SITE_NEW then
               self:queueEvent(HOUND.EVENTS.SITE_CLASSIFIED)
            end
            self.state = HOUND.EVENTS.SITE_UPDATED
        end
    end

    function HOUND.Contact.Site:updatePos()
        local noPos = (self.pos.p == nil)
        self:ensurePrimaryHasPos()
        for _,emitter in ipairs(self.emitters) do
            if emitter:hasPos() then
                self.pos.p = l_mist.utils.deepCopy(emitter:getPos())
                break
            end
        end
        if noPos and self.pos.p ~= nil then
            self:queueEvent(HOUND.EVENTS.SITE_CREATED)
        end
    end

    function HOUND.Contact.Site:ensurePrimaryHasPos(refPos)
        local primary = self:getPrimary()
        if ( not primary:hasPos() ) then
            for _,emitter in ipairs(self.emitters) do
                if ( emitter:hasPos() ) then
                    primary.pos = l_mist.utils.deepCopy(emitter.pos)
                    primary.uncertenty_data = l_mist.utils.deepCopy(emitter.uncertenty_data)
                    break
                end
            end

            if ( not primary:hasPos() and HoundUtils.Dcs.isPoint(refPos)) then
                local uncertenty = primary:getMaxWeaponsRange() * 0.75
                primary.pos.p = l_mist.utils.deepCopy(refPos)
                primary.pos = primary:calculateExtrasPosData(primary.pos)
                primary.uncertenty_data = {}
                primary.uncertenty_data.major = uncertenty
                primary.uncertenty_data.minor = uncertenty
                primary.uncertenty_data.theta = 0
                primary.uncertenty_data.az = 0
                primary.uncertenty_data.r  = uncertenty
            end
        end
    end

    function HOUND.Contact.Site:updateSector()
        for _,emitter in ipairs(self.emitters) do
            if emitter:hasPos() then
                self.threatSectors = emitter.threatSectors
                self.primarySector = emitter.primarySector
                break
            end
        end
        self:updateDefaultSector()
    end

    function HOUND.Contact.Site:LaunchDetected(cooldown)
        local cooldown = cooldown or 30
        if ( HoundUtils.absTimeDelta(self.last_launch_notify) > cooldown ) then

            self.last_launch_notify = timer.getAbsTime()
            local event = {
                id = HOUND.EVENTS.SITE_LAUNCH,
                initiator = self,
                time = timer.getTime()
            }
            return event
        end
    end

    function HOUND.Contact.Site:processData()
        self:update()
    end

    function HOUND.Contact.Site:update()
        if #self.emitters > 0 then
            self:gcEmitters()
            self:selectPrimaryEmitter()
            self:updateTypeAssigned()
            self:updatePos()
            self:updateSector()
            local isPB = false
            for _,emitter in ipairs(self.emitters) do
                self.last_seen = l_math.max(self.last_seen,emitter.last_seen)
                self.maxWeaponsRange = l_math.max(self.maxWeaponsRange,emitter:getMaxWeaponsRange())
                self.detectionRange = l_math.max(self.detectionRange,emitter:getRadarDetectionRange())
                isPB = isPB or emitter:isAccurate()
            end
            self:setPreBriefed(isPB)
        end
        if self.state ~=  HOUND.EVENTS.SITE_ASLEEP then
            if (self:isTimedout() and not self:isAccurate()) or #self.emitters == 0 then
                self.state = HOUND.EVENTS.SITE_ASLEEP
                self:queueEvent(self.state)
            end
        end
        if #self.emitters == 0 and not self:hasRadarUnits() then
            self:queueEvent(HOUND.EVENTS.SITE_REMOVED)
        end
    end

    function HOUND.Contact.Site:drawAreaMarker(numPoints)
        if numPoints == nil then numPoints = 1 end
        if numPoints ~= 1 and numPoints ~= 4 and numPoints ~=8 and numPoints ~= 16 then
            HOUND.Logger.error("DCS limitation, only 1,4,8 or 16 points are allowed")
            numPoints = 1
            end

        local alpha = HoundUtils.Mapping.linear(l_math.floor(HoundUtils.absTimeDelta(self.last_seen)),0,HOUND.CONTACT_TIMEOUT,HOUND.MARKER_MAX_ALPHA,HOUND.MARKER_MIN_ALPHA,true)
        local fillColor = {0,0,0,0}
        local lineColor = {0,0.2,0,alpha}
        local lineType = 4
        if (HoundUtils.absTimeDelta(self.last_seen) < 15) then
            lineType = 3
        end
        if self._platformCoalition == coalition.side.BLUE then
            fillColor[1] = 1
            lineColor[1] = 1
        end

        if self._platformCoalition == coalition.side.RED then
            fillColor[3] = 1
            lineColor[3] = 1
        end

        local markArgs = {
            fillColor = fillColor,
            lineColor = lineColor,
            coalition = self._platformCoalition,
            lineType = lineType
        }
        if numPoints == 1 then
            markArgs.pos = {
                p = self:getPos(),
                r = self.maxWeaponsRange
            }
        else
            markArgs.pos = HOUND.Contact.Emitter.calculatePoly(self.uncertenty_data,numPoints,self.pos.p)
        end
        return self._markpoints.area:update(markArgs)
    end

    function HOUND.Contact.Site:updateMarker(MarkerType)
        if not HoundUtils.Dcs.isPoint(self:getPos()) or type(self.maxWeaponsRange) ~= "number"  then return end
        self._markpoints.area:remove()

        local textColor = 0
        local textAlpha = 1
        if not self:isAccurate() then
            textAlpha = HoundUtils.Mapping.linear(l_math.floor(HoundUtils.absTimeDelta(self.last_seen)),10,HOUND.CONTACT_TIMEOUT,1,0.5,true)
        end
        if self:isTimedout() and not self:isAccurate() then
            textAlpha = 0.5
            Colorfactor = 0.3
        end

        local lineColor = {textColor,textColor,textColor,textAlpha}

        if self._platformCoalition == coalition.side.BLUE then
            lineColor[1] = 0.7
        elseif self._platformCoalition == coalition.side.RED then
            lineColor[3] = 0.7
        end

        local markerArgs = {
            text = self:getName() .. " (" .. self:getDesignation(true).. ")",
            pos = self:getPos(),
            coalition = self._platformCoalition,
            lineColor = lineColor,
            useLegacyMarker = false
        }
        self._markpoints.pos:update(markerArgs)

    end

    function HOUND.Contact.Site:updateMarkers(markerType,drawSite)
        if (type(markerType) ~= "number" or markerType == HOUND.MARKER.NONE) and not drawSite then return end
        if markerType > HOUND.MARKER.SITE_ONLY then
            for _,emitter in pairs(self.emitters) do
                emitter:updateMarker(markerType)
            end
        end
        if drawSite then
            self:updateMarker(HOUND.MARKER.SITE_ONLY)
        end
    end

end--- HOUND.Contact.Site_comms
do
    local l_mist = HOUND.Mist
    local HoundUtils = HOUND.Utils

    function HOUND.Contact.Site:getTextData(utmZone,MGRSdigits)
        local primary = self:getPrimary()
        if not primary:hasPos() then return end
        return primary:getTextData(utmZone,MGRSdigits)
    end

    function HOUND.Contact.Site:getTtsData(utmZone,MGRSdigits)
        local primary = self:getPrimary()
        if not primary:hasPos() then return end
        return primary:getTtsData(utmZone,MGRSdigits)
    end

    function HOUND.Contact.Site:getRadioItemText()
        local primary = self:getPrimary()
        if not primary:hasPos() then return self:getName() end

        local GridPos,BePos = primary:getTextData(true,1)
        BePos = BePos:gsub(" for ","/")
        return self:getName() .. " - BE: " .. BePos .. " (".. GridPos ..")"
    end

    function HOUND.Contact.Site:getRadioItemsText()
        local items = {
            ['dcsName'] = self:getDcsName(),
            ['txt'] = self:getRadioItemText(),
            ['typeAssigned'] = self:getTypeAssigned(),
            ['emitters'] = {}
        }
        for _,emitter in ipairs(self.emitters) do
            if emitter:hasPos() then
                local emitterEntry = {
                    ['dcsName'] = emitter:getDcsName(),
                    ['txt'] = emitter:getRadioItemText()
                }
                if emitter == self.primaryEmitter then
                    emitterEntry.txt = "(*) " .. emitterEntry.txt
                end
                table.insert(items['emitters'],emitterEntry)
            end
        end
        return items
    end

    function HOUND.Contact.Site:generatePopUpReport(isTTS,sectorName)
        local msg = self:getName() .. ", identified as " .. self:getDesignation(true) .. ", is active"

        if sectorName then
            msg = msg .. " in " .. sectorName
        else
            local primary = self:getPrimary()
            if primary:hasPos() then
                local GridPos,BePos
                if isTTS then
                    GridPos,BePos = primary:getTtsData(true,1)
                    msg = msg .. ", bullseye " .. BePos .. ", grid ".. GridPos
                else
                    GridPos,BePos = primary:getTextData(true,1)
                    msg = msg .. " BE: " .. BePos .. " (grid ".. GridPos ..")"
                end
            end
        end
        return msg .. "."
    end

    function HOUND.Contact.Site:generateDeathReport(isTTS,sectorName)
        local msg = self:getName() ..  ", identified as " .. self:getDesignation(true) .. " is down"
        if sectorName then
            msg = msg .. " in " .. sectorName
        else
            if self:hasPos() then
                local GridPos,BePos
                if isTTS then
                    GridPos,BePos = self:getTtsData(true,1)
                    msg = msg .. ", bullseye " .. BePos .. ", grid ".. GridPos
                else
                    GridPos,BePos = self:getTextData(true,1)
                    msg = msg .. " BE: " .. BePos .. " (grid ".. GridPos ..")"
                end
            end
        end
        return msg .. "."
    end

    function HOUND.Contact.Site:generateAsleepReport(isTTS,sectorName)
        local msg = self:getName() ..  ", identified as " .. self:getDesignation(true) .. " is asleep"
        if sectorName then
            msg = msg .. " in " .. sectorName
        else
            if self:hasPos() then
                local GridPos,BePos
                if isTTS then
                    GridPos,BePos = self:getTtsData(true,1)
                    msg = msg .. ", bullseye " .. BePos .. ", grid ".. GridPos
                else
                    GridPos,BePos = self:getTextData(true,1)
                    msg = msg .. " BE: " .. BePos .. " (grid ".. GridPos ..")"
                end
            end
        end
        return msg .. "."
    end

    function HOUND.Contact.Site:generateLaunchAlert(isTTS,sectorName)
    local msg = "SAM LAUNCH! SAM LAUNCH! " .. self:getDesignation(true)
    if sectorName then
        msg = msg .. " in " .. sectorName
    else
        if self:hasPos() then
            local GridPos,BePos
            if isTTS then
                GridPos,BePos = self:getTtsData(true,1)
                msg = msg .. ", bullseye " .. BePos
            else
                GridPos,BePos = self:getTextData(true,1)
                msg = msg .. " BE: " .. BePos .. " (grid ".. GridPos ..")"
            end
        end
    end
    return  msg .. "!"
    end

    function HOUND.Contact.Site:generateIdentReport(isTTS,sectorName)
        local msg = self:getName()

        if sectorName then
            msg = msg .. " in " .. sectorName
            msg = msg .. ", has been reclassified as " .. self:getDesignation(true)
        else
            msg = msg .. ", has been reclassified as " .. self:getDesignation(true)
            local primary = self:getPrimary()
            if primary:hasPos() then
                local GridPos,BePos
                if isTTS then
                    GridPos,BePos = primary:getTtsData(true,1)
                    msg = msg .. ", bullseye " .. BePos .. ", grid ".. GridPos
                else
                    GridPos,BePos = primary:getTextData(true,1)
                    msg = msg .. " BE: " .. BePos .. " (grid ".. GridPos ..")"
                end
            end
        end
        return msg .. "."
    end

    function HOUND.Contact.Site:generateTtsBrief(NATO)
        if self:getType() == "Naval" then
            local boatData = {}
            for _,emitter in ipairs(self:getEmitters()) do
                table.insert(boatData,emitter:generateTtsBrief(NATO))
            end
            return table.concat(boatData," ")
        end
        local str = ""

        local primary = self:getPrimary()
        if getmetatable(primary) ~= HOUND.Contact.Emitter or primary.pos.p == nil or primary.uncertenty_data == nil then return str end
        local phoneticGridPos,phoneticBulls = primary:getTtsData(false,1)
        local reportedName = self:getName() .. " "
        if NATO then
            reportedName = ""
        end
        str = reportedName .. self:getDesignation(NATO)
        if primary:isAccurate() then
            str = str .. ", reported"
        else
            str = str .. ", " .. HoundUtils.TTS.getVerbalContactAge(self.last_seen,true,NATO)
        end
        if NATO then
            str = str .. " bullseye " .. phoneticBulls
        else
            str = str .. " at " .. phoneticGridPos
        end
        if not primary:isAccurate() then
            str = str .. ", accuracy " .. HoundUtils.TTS.getVerbalConfidenceLevel( primary.uncertenty_data.r )
        end
        str = str .. "."
        return str
    end

    function HOUND.Contact.Site:generateIntelBrief()
        if #self.emitters == 0 then return end
        local items = {}

        for _,emitter in ipairs(self.emitters) do
            local body = emitter:generateIntelBrief()
            if body ~= "" then
                local entry = table.concat({self:getName(),self:getDesignation(true),body,self.DcsObjectName},",")
                table.insert(items,entry)
            end
        end
        return items
    end

    function HOUND.Contact.Site:export()
        local report = {
            name = self:getName(),
            DcsObjectName = self:getDcsName(),
            gid = self.gid % 100,
            Type = self:getDesignation(true),
            last_seen = self.last_seen,
            emitters = {}
        }
        if #self.emitters == 0 then return report end
        for _,emitter in ipairs(self.emitters) do
            table.insert(report.emitters,emitter:export())
        end
        return l_mist.utils.deepCopy(report)
    end
end--- Hound Comms Manager (Base class)
do
    local HoundUtils = HOUND.Utils
    HOUND.Comms.Manager = {}
    HOUND.Comms.Manager.__index = HOUND.Comms.Manager

    function HOUND.Comms.Manager:create(sector,houndConfig,settings)
        if (not houndConfig and type(houndConfig) ~= "table") or
            (not sector and type(sector) ~= "string") then
                HOUND.Logger.warn("[Hound] - Comm Controller could not be initilized, missing params")
                return nil
        end
        local CommsManager = {}
        setmetatable(CommsManager, HOUND.Comms.Manager)
        CommsManager.enabled = false
        CommsManager.transmitter = nil
        CommsManager.sector = nil
        CommsManager.houndConfig = houndConfig

        CommsManager._queue = {
            {},{},{}
        }

        CommsManager.settings = {
            freq = 250.000,
            volume = "1.0",
            name = "Hound",
            speed = 0,
            voice = nil,
            gender = nil,
            culture = nil,
            interval = 0.5,
            freqAlias = nil
        }

        CommsManager.preferences = {
            enabletts = true,
            enabletext = false
        }

        if not HoundUtils.TTS.isAvailable() then
            CommsManager.preferences.enabletts = false
        end

        CommsManager.scheduler = nil

        if type(settings) == "table" and HOUND.Length(settings) > 0 then
            CommsManager:updateSettings(settings)
        end
        return CommsManager
    end

    function HOUND.Comms.Manager:updateSettings(settings)
        for k,v in pairs(settings) do
            local k0 = tostring(k):lower()
            if HOUND.setContainsValue({"enabletts","enabletext","alerts"},k0) then
                self.preferences[k0] = v
            else
                self.settings[k0] = v
            end
        end
    end
    function HOUND.Comms.Manager:enable()
        self.enabled = true
        if self.scheduler == nil then
            self.scheduler = timer.scheduleFunction(self.TransmitFromQueue, self, timer.getTime() + self.settings.interval)
        end
        self:startCallbackLoop()
    end

    function HOUND.Comms.Manager:disable()
        if self.scheduler then
            timer.removeFunction(self.scheduler)
            self.scheduler = nil
        end
        self:stopCallbackLoop()
        self.enabled = false
    end

    function HOUND.Comms.Manager:isEnabled()
        return self.enabled
    end

    function HOUND.Comms.Manager:getSettings(key)
        local k0 = tostring(key):lower()
        if HOUND.setContainsValue({"enabletts","enabletext","alerts"},k0) then
            return self.preferences[tostring(key):lower()]
        else
            return self.settings[tostring(key):lower()]
        end
    end

    function HOUND.Comms.Manager:setSettings(key,value)
        local k0 = tostring(key):lower()
        if HOUND.setContainsValue({"enabletts","enabletext","alerts"},k0) then
            self.preferences[k0] = value
        else
            self.settings[k0] = value
        end
    end

    function HOUND.Comms.Manager:enableText()
        self:setSettings("enableText",true)
    end

    function HOUND.Comms.Manager:disableText()
        self:setSettings("enableText",false)
    end

    function HOUND.Comms.Manager:enableTTS()
        if HoundUtils.TTS.isAvailable() then
            self:setSettings("enableTTS",true)
        end
    end

    function HOUND.Comms.Manager:disableTTS()
        self:setSettings("enableTTS",false)
    end

    function HOUND.Comms.Manager:enableAlerts()
        self:setSettings("alerts",true)
    end

    function HOUND.Comms.Manager:disableAlerts()
        self:setSettings("alerts",false)
    end

    function HOUND.Comms.Manager:setTransmitter(transmitterName)
        if not transmitterName then transmitterName = "" end
        local candidate = Unit.getByName(transmitterName)
        if not HoundUtils.Dcs.isUnit(candidate) then
            candidate = StaticObject.getByName(transmitterName)
        end
        if not HoundUtils.Dcs.isStaticObject(candidate) and self.transmitter then
            self:removeTransmitter()
            return
        end
        if self.transmitter ~= candidate then
            self.transmitter = candidate
            HOUND.EventHandler.publishEvent({
                    id = HOUND.EVENTS.TRANSMITTER_ADDED,
                    houndId = self.houndConfig:getId(),
                    initiator = self.sector,
                    transmitter = candidate
                })
        end
    end

    function HOUND.Comms.Manager:removeTransmitter()
        if self.transmitter ~= nil then
            self.transmitter = nil
            HOUND.EventHandler.publishEvent({
                    id = HOUND.EVENTS.TRANSMITTER_REMOVED,
                    houndId = self.houndConfig:getId(),
                    initiator = self.sector
                })
        end
    end

    function HOUND.Comms.Manager:getCallsign()
        return self:getSettings("name")
    end

    function HOUND.Comms.Manager:setCallsign(callsign)
        if type(callsign) == "string" then
            self:setSettings("name",callsign)
        end
    end

    function HOUND.Comms.Manager:getFreq()
        return self:getFreqs()[1]
    end

    function HOUND.Comms.Manager:getFreqs()
        local freqs = string.split(self.settings.freq,",")
        local mod = string.split(self.settings.modulation,",")
        local retval = {}

        for i,freq in ipairs(freqs) do
            local str = string.format("%.3f",tonumber(freq)) .. " " .. (mod[i] or HoundUtils.TTS.getdefaultModulation(freq))
            table.insert(retval,str)
        end
        return retval
    end

    function HOUND.Comms.Manager:getAlias()
        return self:getSettings("freqAlias")
    end

    function HOUND.Comms.Manager:setAlias(alias)
        if type(alias) == "string" then
            self:setSettings("freqAlias",alias)
        end
    end

    function HOUND.Comms.Manager:addMessageObj(obj)
        if obj.coalition == nil or not self.enabled then return end
        if obj.txt == nil and obj.tts == nil then return end
        if obj.priority == nil or obj.priority > 3 or obj.priority < 0 then obj.priority = 3 end
        if obj.priority == 0 then
            obj.priority = 1
            obj.push = true
        end
        if obj.priority == "loop" then
            self.loop.msg = obj
            return
        end
        if obj.gid and type(obj.gid) ~= "table" then
            obj.gid = {obj.gid}
        end
        if obj.contactId ~= nil then
            for id,queueObj in ipairs(self._queue[obj.priority]) do
                if obj.gid == queueObj.gid and obj.contactId == queueObj.contactId then
                    self._queue[obj.priority][id].txt = obj.txt
                    self._queue[obj.priority][id].tts = obj.tts
                    return
                end
            end
        end
        if obj.push then
            table.insert(self._queue[obj.priority],1,obj)
        else
            table.insert(self._queue[obj.priority],obj)
        end
    end

    function HOUND.Comms.Manager:addMessage(coalition,msg,prio)
        if msg == nil or coalition == nil or ( type(msg) ~= "string" and string.len(tostring(msg)) <= 0) or not self.enabled then return end
        if prio == nil or prio > 3 or prio < 0 then prio = 3 end

        local obj = {
            coalition = coalition,
            tts = msg,
            priority = prio
        }
        self:addMessageObj(obj)
    end

    function HOUND.Comms.Manager:addTxtMsg(coalition,msg,prio)
        if msg == nil or string.len(tostring(msg)) == 0 or coalition == nil  or not self.enabled then return end
        if prio == nil then prio = 1 end
        local obj = {
            coalition = coalition,
            priority = prio,
            txt = msg
        }
        self:addMessageObj(obj)
    end

    function HOUND.Comms.Manager:getNextMsg()
        for i,v in ipairs(self._queue) do
            if #v > 0 then return table.remove(self._queue[i],1) end
        end
    end

    function HOUND.Comms.Manager:getTransmitterPos()
        if self.transmitter == nil then return nil end
        if self.transmitter ~= nil and (self.transmitter:isExist() == false or self.transmitter:getLife() < 1) then
            return false
        end
        local pos = self.transmitter:getPoint()
        local transmitterObjectCat, transmitterSubCat = self.transmitter:getCategory()
        if transmitterObjectCat == Object.Category.STATIC or (transmitterObjectCat == Object.Category.UNIT and transmitterSubCat == Unit.Category.GROUND_UNIT) then
            local verticalOffset = (self.transmitter:getDesc()["box"]["max"]["y"] + 5) or 20
            pos.y = pos.y + verticalOffset
        end
        return pos
    end

    function HOUND.Comms.Manager.TransmitFromQueue(gSelf)
        local msgObj = gSelf:getNextMsg()
        local readTime = gSelf.settings.interval
        if msgObj == nil then return timer.getTime() + readTime end

        local transmitterPos = gSelf:getTransmitterPos()

        if transmitterPos == false then
            env.info("[Hound] - Transmitter destroyed")
            HOUND.EventHandler.publishEvent({
                    id = HOUND.EVENTS.TRANSMITTER_DESTROYED,
                    houndId = gSelf.houndConfig:getId(),
                    initiator = gSelf.sector,
                    transmitter = gSelf.transmitter
                })

            return timer.getTime() + 10
        end

        if gSelf.enabled and HoundUtils.TTS.isAvailable() and msgObj.tts ~= nil and gSelf.preferences.enabletts then
            HoundUtils.TTS.Transmit(msgObj.tts,msgObj.coalition,gSelf.settings,transmitterPos)
            readTime = HoundUtils.TTS.getReadTime(msgObj.tts,gSelf.settings.speed,gSelf.settings.googletts)

        end

        if gSelf.enabled and gSelf.preferences.enabletext and msgObj.txt ~= nil then
            readTime =  HoundUtils.TTS.getReadTime(msgObj.tts,gSelf.settings.speed) or HoundUtils.TTS.getReadTime(msgObj.txt,gSelf.settings.speed)
            if msgObj.gid then
                for _,gid in ipairs(msgObj.gid) do
                    trigger.action.outTextForGroup(gid,msgObj.txt,readTime+2)
                end
            else
                trigger.action.outTextForCoalition(msgObj.coalition,msgObj.txt,readTime+2)
            end
        end
        return timer.getTime() + readTime + gSelf.settings.interval
    end

    function HOUND.Comms.Manager:startCallbackLoop()
        return nil
    end

    function HOUND.Comms.Manager:stopCallbackLoop()
        return nil
    end

    function HOUND.Comms.Manager:SetMsgCallback()
        return nil
    end

    function HOUND.Comms.Manager:runCallback()
        return nil
    end
end

do

    HOUND.Comms.InformationSystem = {}
    HOUND.Comms.InformationSystem = HOUND.inheritsFrom(HOUND.Comms.Manager)

    function HOUND.Comms.InformationSystem:create(sector,houndConfig,settings)
        local instance = self:superClass():create(sector,houndConfig,settings)
        setmetatable(instance, HOUND.Comms.InformationSystem)
        self.__index = self

        instance.settings.freq = 250.500
        instance.settings.interval = 4
        instance.settings.speed = 1
        instance.preferences.reportewr = false

        if settings and type(settings) == "table" then
            instance:updateSettings(settings)
        end

        instance.callback = {
            scheduler = nil,
            func = nil,
            args = nil,
            interval = 300
        }

        instance.loop = {
            body = "",
            msg = nil,
            reportIdx = 'Z'
        }

        return instance
    end

    function HOUND.Comms.InformationSystem:reportEWR(state)
        if type(state) == "boolean" then
            self:setSettings("reportEWR",state)
        end
    end

    function HOUND.Comms.InformationSystem:startCallbackLoop()
        if self.enabled and not self.callback.scheduler then
            self.callback.scheduler = timer.scheduleFunction(self.runCallback, self, timer.getTime()+0.1)
        end
    end

    function HOUND.Comms.InformationSystem:stopCallbackLoop()
        if self.callback.scheduler then
            timer.removeFunction(self.callback.scheduler)
            self.callback.scheduler = nil
        end
        self.loop.msg = nil
        self.loop.header = ""
        self.loop.body = ""
        self.loop.footer = ""
        self.callback = {}
    end

    function HOUND.Comms.InformationSystem:SetMsgCallback(callback,args)
        if callback ~= nil and type(callback) == "function" then
            self.callback.func = callback
            self.callback.args = args
            self.callback.interval = self.houndConfig:getAtisUpdateInterval()
        end
        if self.callback.scheduler == nil and self.scheduler ~= nil then
            self.startCallbackLoop()
        end
    end

    function HOUND.Comms.InformationSystem:runCallback()
        local nextDelay = self.callback.interval or 300
        if self.callback ~= nil and type(self.callback.func) == "function"  then
            self.callback.func(self.callback.args,self.loop,self.preferences)
        end
        return timer.getTime() + nextDelay
    end

    function HOUND.Comms.InformationSystem:getNextMsg()
        if self.loop and not self.loop.msg then
            self:runCallback()
        end
        if self.loop and self.loop.msg and self.loop.msg.tts ~= nil and (string.len(self.loop.msg.tts) > 0 or string.len(self.loop.msg.txt) > 0) then
            return self.loop.msg
        end
    end
end

do

    HOUND.Comms.Controller = {}
    HOUND.Comms.Controller = HOUND.inheritsFrom(HOUND.Comms.Manager)

    function HOUND.Comms.Controller:create(sector,houndConfig,settings)
        local instance = self:superClass():create(sector,houndConfig,settings)
        setmetatable(instance, HOUND.Comms.Controller)
        self.__index = self

        instance.preferences.alerts = true

        if settings and type(settings) == "table" then
            instance:updateSettings(settings)
        end

        return instance
    end
end
do
    HOUND.Comms.Notifier = {}
    HOUND.Comms.Notifier = HOUND.inheritsFrom(HOUND.Comms.Manager)

    function HOUND.Comms.Notifier:create(sector,houndConfig,settings)
        local instance = self:superClass():create(sector,houndConfig,settings)
        setmetatable(instance, HOUND.Comms.Notifier)
        self.__index = self

        instance.settings.freq = "243.000,121.500"
        instance.settings.modulation = "AM,AM"
        instance.settings.speed = 1

        instance.preferences.alerts = true

        if settings and type(settings) == "table" then
            instance:updateSettings(settings)
        end
        return instance
    end
end
do
    local HoundUtils = HOUND.Utils

    HOUND.ElintWorker = {}
    HOUND.ElintWorker.__index = HOUND.ElintWorker

    local l_math = math
    function HOUND.ElintWorker.create(HoundInstanceId)
        local instance = {}
        setmetatable(instance, HOUND.ElintWorker)

        instance.contacts = {}
        instance.platforms = {}
        instance.sites = {}
        instance.settings =  HOUND.Config.get(HoundInstanceId)
        instance.coalitionId = nil
        instance.TrackIdCounter = 0
        return instance
    end

    function HOUND.ElintWorker:setCoalition(coalitionId)
        if not coalitionId then return false end
        if not self.settings:getCoalition() then
            self.settings:setCoalition(coalitionId)
            return true
        end
        return false
    end

    function HOUND.ElintWorker:getCoalition()
        return self.settings:getCoalition()
    end

    function HOUND.ElintWorker:getNewTrackId()
        self.TrackIdCounter = self.TrackIdCounter + 1
        return self.TrackIdCounter
    end

    function HOUND.ElintWorker:addPlatform(platformName)
        local candidate = Unit.getByName(platformName) or StaticObject.getByName(platformName)
        if HOUND.Utils.Dcs.isUnit(platformName) or HOUND.Utils.Dcs.isStaticObject(platformName) then
            candidate = platformName
        end

        if not (HOUND.Utils.Dcs.isUnit(candidate) or HOUND.Utils.Dcs.isStaticObject(candidate)) then
            HOUND.Logger.warn("Failed to add platform "..platformName..". Could not find the Object.")
            return false
        end
        if self:getCoalition() == nil and candidate ~= nil then
            self:setCoalition(candidate:getCoalition())
        end

        if candidate ~= nil and candidate:getCoalition() == self:getCoalition()
            and not HOUND.setContainsValue(self.platforms,candidate) and HOUND.DB.isValidPlatform(candidate) then
                table.insert(self.platforms, candidate)
                HOUND.EventHandler.publishEvent({
                    id = HOUND.EVENTS.PLATFORM_ADDED,
                    initiator = candidate,
                    houndId = self.settings:getId(),
                    coalition = self.settings:getCoalition()
                })
                return true
        end
        if candidate ~= nil and HOUND.setContainsValue(self.platforms,candidate) then
            HOUND.Logger.debug("[Hound] - "..platformName.." Already registered platform.")
            return false
        end
        HOUND.Logger.warn("[Hound] - Failed to add platform "..platformName..". Make sure you use unit name and that all requirments are met.")
        return false
    end

    function HOUND.ElintWorker:removePlatform(platformName)
        local candidate = Unit.getByName(platformName)
        if candidate == nil then
            candidate = StaticObject.getByName(platformName)
        end

        if candidate ~= nil then
            for k,v in ipairs(self.platforms) do
                if v == candidate then
                    table.remove(self.platforms, k)
                    HOUND.EventHandler.publishEvent({
                        id = HOUND.EVENTS.PLATFORM_REMOVED,
                        initiator = candidate,
                        houndId = self.settings:getId(),
                        coalition = self.settings:getCoalition()
                    })
                    return true
                end
            end
        end
        return false
    end

    function HOUND.ElintWorker:platformRefresh()
        if HOUND.Length(self.platforms) < 1 then return end
        for id,platform in ipairs(self.platforms) do
            if platform:isExist() == false or platform:getLife() <1 then
                table.remove(self.platforms, id)
                HOUND.EventHandler.publishEvent({
                    id = HOUND.EVENTS.PLATFORM_DESTROYED,
                    initiator = platform,
                    houndId = self.settings:getId(),
                    coalition = self.settings:getCoalition()
                })
            end
        end
    end

    function HOUND.ElintWorker:removeDeadPlatforms()
        if HOUND.Length(self.platforms) < 1 then return end
        for id,platform in ipairs(self.platforms) do
            if platform:isExist() == false or platform:getLife() <1  or (platform:getCategory() ~= Object.Category.STATIC and platform:isActive() == false) then
                table.remove(self.platforms, id)
                HOUND.EventHandler.publishEvent({
                    id = HOUND.EVENTS.PLATFORM_DESTROYED,
                    initiator = platform,
                    houndId = self.settings:getId(),
                    coalition = self.settings:getCoalition()
                })
            end
        end
    end

    function HOUND.ElintWorker:countPlatforms()
        return HOUND.Length(self.platforms)
    end

    function HOUND.ElintWorker:listPlatforms()
        local platforms = {}
        for _,platform in ipairs(self.platforms) do
            table.insert(platforms,platform:getName())
        end
        return platforms
    end

    function HOUND.ElintWorker:isContact(emitter)
        if emitter == nil then return false end
        local emitterName = nil
        if type(emitter) == "string" then
            emitterName = emitter
        end
        if type(emitter) == "table" and emitter.getName ~= nil then
            emitterName = emitter:getName()
        end
        return HOUND.setContains(self.contacts,emitterName)
    end

    function HOUND.ElintWorker:addContact(emitter)
        if emitter == nil or emitter.getName == nil then return end
        local emitterName = emitter:getName()
        if self.contacts[emitterName] ~= nil then return emitterName end
        self.contacts[emitterName] = HOUND.Contact.Emitter:New(emitter, self:getCoalition(), self:getNewTrackId())
        local site = self:getSite(self.contacts[emitterName])
        if site then
            site:addEmitter(self.contacts[emitterName])
        else
            HOUND.Logger.debug("failed to create site")
        end
        self.contacts[emitterName]:queueEvent(HOUND.EVENTS.RADAR_NEW)
        return emitterName
    end

    function HOUND.ElintWorker:getContact(emitter,getOnly)
        if emitter == nil then return nil end
        local emitterName = nil
        if type(emitter) == "string" then
            emitterName = emitter
        end
        if HoundUtils.Dcs.isUnit(emitter) then
            emitterName = emitter:getName()
        end
        if getmetatable(emitter) == HOUND.Contact.Emitter then
            emitterName = emitter:getDcsName()
        end
        if emitterName ~= nil and self.contacts[emitterName] ~= nil then return self.contacts[emitterName] end
        if not self.contacts[emitterName] and type(emitter) == "table" and not getOnly then
            self:addContact(emitter)
            return self.contacts[emitterName]
        end
        return nil
    end

    function HOUND.ElintWorker:removeContact(emitterName)
        if type(emitterName) == "table" and getmetatable(emitterName) == HOUND.Contact.Emitter then
            emitterName = emitterName:getDcsName()
        end
        if type(emitterName) ~= "string" then return false end
        if self.contacts[emitterName] then
            local site = self:getSite(self.contacts[emitterName]:getDcsGroupName(),true)
            if site then
                site:removeEmitter(self.contacts[emitterName])
            end

            self.contacts[emitterName]:updateDeadDcsObject()
        end
        self.contacts[emitterName] = nil
        return true
    end

    function HOUND.ElintWorker:setPreBriefedContact(emitter)
        if not emitter:isExist() then return end
        local contact = self:getContact(emitter)
        local contactState = contact:useUnitPos(l_math.min(self.settings:getMarkerType(),HOUND.MARKER.POINT))
        if contactState then
            HOUND.EventHandler.publishEvent({
                id = contactState,
                initiator = contact,
                houndId = self.settings:getId(),
                coalition = self.settings:getCoalition()
            })
        end
    end

    function HOUND.ElintWorker:setDead(emitter)
        local contact = self:getContact(emitter,true)
        if contact then
            contact:setDead()
         end
    end

    function HOUND.ElintWorker:ensureSitePrimaryHasPos(fireGrp,refPos)
        local site = self:getSite(fireGrp,true)
        if site then
            site:ensurePrimaryHasPos(refPos)
        end
    end
    function HOUND.ElintWorker:AlertOnLaunch(fireGrp)
        if not self.settings:getAlertOnLaunch() then return end
        local site = self:getSite(fireGrp,true)
        if site then
            local event = site:LaunchDetected()
            if type(event) == "table" then
                event.houndId = self.settings:getId()
                event.coalition = self.settings:getCoalition()
                HOUND.EventHandler.publishEvent(event)
            end
        end
    end

    function HOUND.ElintWorker:isTracked(emitter)
        if emitter == nil then return false end
        if type(emitter) =="string" and self.contacts[emitter] ~= nil then return true end
        if type(emitter) == "table" and emitter.getName ~= nil and self.contacts[emitter:getName()] ~= nil then return true end
        return false
    end

    function HOUND.ElintWorker:isSite(site)
        if site == nil then return false end
        local groupName = nil
        if type(site) == "string" then
            groupName = site
        end
        if HOUND.Utils.Dcs.isGroup(site) then
            groupName = site:getName()
        end
        return HOUND.setContains(self.sites,groupName)
    end

    function HOUND.ElintWorker:addSite(emitter)
        if emitter == nil or emitter.getName == nil then return end
        local groupName = emitter:getDcsGroupName()
        if self.sites[groupName] ~= nil then return groupName end
        self.sites[groupName] = HOUND.Contact.Site:New(emitter, self:getCoalition(), self:getNewTrackId())
        self.sites[groupName]:queueEvent(HOUND.EVENTS.SITE_NEW)
        return groupName
    end

    function HOUND.ElintWorker:getSite(emitter,getOnly)
        if emitter == nil then return nil end
        local groupName = nil
        if type(emitter) == "string" then
            groupName = emitter
        end
        if HOUND.Utils.Dcs.isGroup(emitter) then
            groupName = emitter:getName()
        elseif HOUND.Utils.Dcs.isUnit(emitter) then
            groupName = Group.getName(emitter:getGroup())
        end
        if getmetatable(emitter) == HOUND.Contact.Emitter then
            groupName = emitter:getDcsGroupName()
        end
        if groupName ~= nil and self.sites[groupName] ~= nil then return self.sites[groupName] end
        if not self.sites[groupName] and type(emitter) == "table" and not getOnly then
            self:addSite(emitter)
            return self.sites[groupName]
        end
        return nil
    end

    function HOUND.ElintWorker:removeSite(groupName)
        if type(groupName) == "table" and getmetatable(groupName) == HOUND.Contact.Site then
            groupName = groupName:getDcsName()
        end
        if type(groupName) ~= "string" then return false end
        self.sites[groupName] = nil
        return true
    end

    function HOUND.ElintWorker:UpdateMarkers()
        if self.settings:getUseMarkers() then
            local drawSites = self.settings:getMarkSites()
            local emitterMarker = self.settings:getMarkerType()
            for _,site in pairs(self.sites) do
                site:updateMarkers(emitterMarker,drawSites)
            end
        end
    end

    function HOUND.ElintWorker:Sniff(GroupName)
        self:removeDeadPlatforms()

        for _, contact in pairs(self.contacts) do
            contact:KalmanPredict()
        end
        if HOUND.Length(self.platforms) == 0 then return end
        local Radars = {}
        if GroupName then
            Radars = HoundUtils.Elint.getActiveRadarsInGroup(GroupName)
        else
            Radars = HoundUtils.Elint.getActiveRadars(self:getCoalition())
        end
        if HOUND.Length(Radars) == 0 then return end
        for _,RadarName in ipairs(Radars) do
            local radar = Unit.getByName(RadarName)
            local radarPos = radar:getPosition().p
            radarPos.y = radarPos.y + radar:getDesc()["box"]["max"]["y"] -- use vehicle bounting box for height
            local _,isRadarTracking = radar:getRadar()

            isRadarTracking = HoundUtils.Dcs.isUnit(isRadarTracking)

            for _,platform in ipairs(self.platforms) do
                local platformData = HOUND.DB.getPlatformData(platform)

                if HoundUtils.Geo.checkLOS(platformData.pos, radarPos) then
                    local contact = self:getContact(radar)
                    local sampleAngularResolution = HOUND.DB.getSensorPrecision(platform,contact:getWavelenght(isRadarTracking))
                    if sampleAngularResolution < l_math.rad(10.0) then
                        local az,el = HoundUtils.Elint.getAzimuth( platformData.pos, radarPos, sampleAngularResolution )
                        if not platformData.isAerial then
                            el = nil
                        end

                        if not platform.isStatic and self.settings:getPosErr() then
                            for axis,value in pairs(platformData.pos) do
                                platformData.pos[axis] = value + platformData.posErr[axis]
                            end
                        end
                        local signalStrength = HoundUtils.Elint.getSignalStrength(platformData.pos,radarPos,contact.detectionRange)
                        local datapoint = HOUND.Contact.Datapoint.New(platform,platformData.pos, az, el, signalStrength, timer.getAbsTime(),sampleAngularResolution,platformData.isStatic)
                        contact:AddPoint(datapoint)
                    end
                end
            end
        end
    end

    function HOUND.ElintWorker:Process()
        if HOUND.Length(self.contacts) < 1 then return end
        for contactName, contact in pairs(self.contacts) do
            if contact ~= nil then
                local contactState = contact:processData()
                if contactState == HOUND.EVENTS.RADAR_DETECTED then
                    if self.settings:getUseMarkers() then
                        contact:updateMarker(self.settings:getMarkerType())
                    end
                end

                if contact:isTimedout() and not contact:getPreBriefed() then
                    contactState = contact:CleanTimedout()
                end
                if self.settings:getBDA() and contact:isAlive() and contact:getLife() < 1 then
                    contact:setDead()
                end
                if not contact:isAlive() and (contact:getLastSeen() > 60 or contact:getPreBriefed()) then
                    contact:destroy()
                end

                contactState = contact:getState()
                if contactState and contactState ~= HOUND.EVENTS.NO_CHANGE then
                    local contactEvents = contact:getEventQueue()
                    while #contactEvents > 0 do
                        local event = table.remove(contactEvents,1)
                        event.houndId = self.settings:getId()
                        event.coalition = self.settings:getCoalition()
                        HOUND.EventHandler.publishEvent(event)
                    end
                end
            end
        end
        for _, site in pairs(self.sites) do
            if site ~= nil then
                site:processData()
                local siteEvents = site:getEventQueue()
                while #siteEvents > 0 do
                    local event = table.remove(siteEvents,1)
                    event.houndId = self.settings:getId()
                    event.coalition = self.settings:getCoalition()
                    HOUND.EventHandler.publishEvent(event)
                end
            end
        end
    end
end
do
    local HoundUtils = HOUND.Utils

    function HOUND.ElintWorker:listContactsInSector(sectorName)
        local emitters = {}
        for _,emitter in ipairs(self.contacts) do
            if emitter:isInSector(sectorName) then
                table.insert(emitters,emitter)
            end
        end
        table.sort(emitters,HoundUtils.Sort.ContactsByRange)
        return emitters
    end

    function HOUND.ElintWorker:listAllContacts(sectorName)
        if sectorName then
            local contacts = {}
            for _,emitter in pairs(self.contacts) do
                if emitter:isInSector(sectorName) then
                        table.insert(contacts,emitter)
                end
            end
            return contacts
        end
        return self.contacts
    end

    function HOUND.ElintWorker:listAllContactsByRange(sectorName)
        return self:sortContacts(HoundUtils.Sort.ContactsByRange,sectorName)
    end

    function HOUND.ElintWorker:countContacts(sectorName)
        if sectorName then
            local contacts = 0
            for _,contact in pairs(self.contacts) do
                if contact:isInSector(sectorName) then
                    contacts = contacts + 1
                end
            end
            return contacts
        end
        return HOUND.Length(self.contacts)
    end

    function HOUND.ElintWorker:getContacts(sectorName)
        local contacts = {}
        for _,emitter in pairs(self.contacts) do
            if sectorName then
                if emitter:isInSector(sectorName) then
                    table.insert(contacts,emitter)
                end
            else
                table.insert(contacts,emitter)
            end
        end
        return contacts
    end

    function HOUND.ElintWorker:sortContacts(sortFunc,sectorName)
        if type(sortFunc) ~= "function" then return end
        local sorted = self:getContacts(sectorName)
        table.sort(sorted, sortFunc)
        return sorted
    end

    function HOUND.ElintWorker:countSites(sectorName)
        if sectorName then
            local sites = 0
            for _,site in pairs(self.sites) do
                if site:isInSector(sectorName) then
                    sites = sites + 1
                end
            end
            return sites
        end
        return HOUND.Length(self.sites)
    end

    function HOUND.ElintWorker:getSites(sectorName)
        local sites = {}
        for _,site in pairs(self.sites) do
            if sectorName then
                if site:isInSector(sectorName) then
                    table.insert(sites,site)
                end
            else
                table.insert(sites,site)
            end
        end
        return sites
    end

    function HOUND.ElintWorker:sortSites(sortFunc,sectorName)
        if type(sortFunc) ~= "function" then return end
        local sorted = self:getSites(sectorName)
        table.sort(sorted, sortFunc)
        return sorted
    end

    function HOUND.ElintWorker:listAllSites(sectorName)
        if sectorName then
            local sites = {}
            for _,site in pairs(self.sites) do
                if site:isInSector(sectorName) then
                        table.insert(sites,site)
                end
            end
            return sites
        end
        return self.sites
    end

    function HOUND.ElintWorker:listAllSitesByRange(sectorName)
        return self:sortSites(HoundUtils.Sort.ContactsByRange,sectorName)
    end

end    --- HOUND.ContactManager
do
    HOUND.ContactManager = {
        _workers = {}
    }

    HOUND.ContactManager.__index = HOUND.ContactManager

    function HOUND.ContactManager.get(HoundInstanceId)
        if HOUND.ContactManager._workers[HoundInstanceId] then
            return HOUND.ContactManager._workers[HoundInstanceId]
        end

        local worker = HOUND.ElintWorker.create(HoundInstanceId)
        HOUND.ContactManager._workers[HoundInstanceId] = worker

        return HOUND.ContactManager._workers[HoundInstanceId]
    end
end
do
    local l_mist = HOUND.Mist
    local l_math = math
    local HoundUtils = HOUND.Utils

    HOUND.Sector = {}
    HOUND.Sector.__index = HOUND.Sector

    function HOUND.Sector.create(HoundId, name, settings, priority)
        if type(HoundId) ~= "number" or type(name) ~= "string" then
            HOUND.Logger.warn("[Hound] - HOUND.Sector.create() missing params")
            return
        end

        local instance = {}
        setmetatable(instance, HOUND.Sector)
        instance.name = name
        instance._hSettings = HOUND.Config.get(HoundId)
        instance._contacts = HOUND.ContactManager.get(HoundId)
        instance.callsign = "HOUND"
        instance.settings = {
            controller = nil,
            atis = nil,
            notifier = nil,
            transmitter = nil,
            zone = nil,
            hound_menu = nil
        }
        instance.comms = {
            controller = nil,
            atis = nil,
            notifier = nil,
            enrolled = {},
            menu = {
                root = nil ,noData = nil
            }
        }
        instance.priority = priority or 10

        if settings ~= nil and type(settings) == "table" and HOUND.Length(settings) > 0 then
            instance:updateSettings(settings)
        end
        if instance.name ~= "default" then
            instance:setCallsign(instance._hSettings:getUseNATOCallsigns())
        end
        return instance
    end

    function HOUND.Sector:updateSettings(settings)
        for k, v in pairs(settings) do
            local k0 = tostring(k):lower()
            if type(v) == "table" and
                HOUND.setContainsValue({"controller", "atis", "notifier"}, k0) then
                if not self.settings[k0] then
                    self.settings[k0] = {}
                end
                for k1, v1 in pairs(v) do
                    self.settings[k0][tostring(k1):lower()] = v1
                end
                self.settings[k0]["name"] = self.callsign
            else
                self.settings[k0] = v
            end
        end
        self:updateServices()
    end

    function HOUND.Sector:destroy()
        self:removeRadioMenu()
        for _,contact in pairs(self._contacts:listAllContacts()) do
            contact:removeSector(self.name)
        end
        return
    end

    function HOUND.Sector:updateServices()
        if type(self.settings.controller) == "table" then
            if not self.comms.controller then
                self.settings.controller.name = self.callsign
                self.comms.controller = HOUND.Comms.Controller:create(self.name,self._hSettings,self.settings.controller)
            else
                self.settings.controller.name = self.callsign
                self.comms.controller:updateSettings(self.settings.controller)
                self.comms.controller:setCallsign(self.callsign)

            end
        end
        if type(self.settings.atis) == "table" then
            if not self.comms.atis then
                self.settings.atis.name = self.callsign
                self.comms.atis = HOUND.Comms.InformationSystem:create(self.name,self._hSettings,self.settings.atis)
            else
                self.settings.atis.name = self.callsign
                self.comms.atis:updateSettings(self.settings.atis)
                self.comms.atis:setCallsign(self.callsign)
            end
        end
        if type(self.settings.notifier) == "table" then
            if not self.comms.notifier then
                self.settings.notifier.name = self.callsign
                self.comms.notifier = HOUND.Comms.Notifier:create(self.name,self._hSettings,self.settings.notifier)
            else
                self.settings.notifier.name = self.callsign
                self.comms.notifier:updateSettings(self.settings.notifier)
                self.comms.notifier:setCallsign(self.callsign)
            end
        end
        if self.settings.zone and type(self.settings.zone) ~= "table" then
            self:setZone(self.settings.zone)
        end
        if self.settings.transmitter then
            self:updateTransmitter()
        end
    end

    function HOUND.Sector:getName()
        return self.name
    end

    function HOUND.Sector:getPriority()
        return self.priority
    end

    function HOUND.Sector:setCallsign(callsign, NATO)
        local namePool = "GENERIC"
        if callsign ~= nil and type(callsign) == "boolean" then
            NATO = callsign
            callsign = nil
        end
        if NATO == true then namePool = "NATO" end

        callsign = string.upper(callsign or HoundUtils.getHoundCallsign(namePool))

        while HOUND.setContainsValue(self._hSettings.callsigns, callsign) do
            callsign = HoundUtils.getHoundCallsign(namePool)
        end

        if self.callsign ~= nil or self.callsign ~= "HOUND" then
            for k, v in ipairs(self._hSettings.callsigns) do
                if v == self.callsign then
                    table.remove(self._hSettings.callsigns, k)
                end
            end
        end
        table.insert(self._hSettings.callsigns, callsign)
        self.callsign = callsign
        self:updateServices()
    end

    function HOUND.Sector:getCallsign()
        return self.callsign
    end

    function HOUND.Sector:getZone()
        return self.settings.zone
    end

    function HOUND.Sector:hasZone()
        return self:getZone() ~= nil
    end

    function HOUND.Sector:setZone(zonecandidate)
        if self.name == "default" then
            HOUND.Logger.warn("[Hound] - cannot set zone to default sector")
            return
        end
        local zone = nil
        if not zonecandidate then
            zone = HoundUtils.Zone.getDrawnZone(self.name .. " Sector")
        end
        if type(zonecandidate) == "string" then
            zone = HoundUtils.Zone.getDrawnZone(zonecandidate) or HoundUtils.Zone.getGroupRoute(zonecandidate)
        end
        if zone then
            self.settings.zone = zone
        end
    end

    function HOUND.Sector:removeZone() self.settings.zone = nil end

    function HOUND.Sector:setTransmitter(userTransmitter)
        if not userTransmitter then return end
        self.settings.transmitter = userTransmitter
        self:updateTransmitter()
    end

    function HOUND.Sector:updateTransmitter()
        for k, v in pairs(self.comms) do
            if k ~= "menu" and v.setTransmitter then v:setTransmitter(self.settings.transmitter) end
        end
    end

    function HOUND.Sector:removeTransmitter()
        self.settings.transmitter = nil
        for k, v in pairs(self.comms) do
            if k ~= "menu" then v:removeTransmitter() end
        end
    end

    function HOUND.Sector:enableController(userSettings)
        if not userSettings then userSettings = {} end
        local settings = { controller = userSettings }
        self:updateSettings(settings)
        self:updateTransmitter()
        self.comms.controller:enable()
        self:populateRadioMenu()
    end

    function HOUND.Sector:disableController()
        if self.comms.controller then
            self:removeRadioMenu()
            self.comms.controller:disable()
        end
    end

    function HOUND.Sector:removeController()
        self.settings.controller = nil
        if self.comms.controller then
            self:disableController()
            self.comms.controller = nil
        end
    end

    function HOUND.Sector:getControllerFreq()
        if self.comms.controller then
            return self.comms.controller:getFreqs()
        end
        return {}
    end

    function HOUND.Sector:hasController() return self.comms.controller ~= nil end

    function HOUND.Sector:isControllerEnabled()
        return self.comms.controller ~= nil and self.comms.controller:isEnabled()
    end

    function HOUND.Sector:getController()
        if self:hasController() then
            return self.comms.controller
        end
        return
    end

    function HOUND.Sector:transmitOnController(msg,priority)
        if not self.comms.controller or not self.comms.controller:isEnabled() then return end
        if type(msg) ~= "string" then return end
        if type(priority) ~= "number" then priority = 1 end
        local msgObj = {priority = priority,coalition = self._hSettings:getCoalition()}
        msgObj.tts = msg
        if self.comms.controller:isEnabled() then
            self.comms.controller:addMessageObj(msgObj)
        end
    end

    function HOUND.Sector:enableText()
        if self.comms.controller then self.comms.controller:enableText() end
    end

    function HOUND.Sector:disableText()
        if self.comms.controller then self.comms.controller:disableText() end
    end

    function HOUND.Sector:enableAlerts()
        if self.comms.controller then self.comms.controller:enableAlerts() end
    end

    function HOUND.Sector:disableAlerts()
        if self.comms.controller then self.comms.controller:disableAlerts() end
    end

    function HOUND.Sector:enableTTS()
        if self.comms.controller then self.comms.controller:enableTTS() end
    end

    function HOUND.Sector:disableTTS()
        if self.comms.controller then self.comms.controller:disableTTS() end
    end

    function HOUND.Sector:enableAtis(userSettings)
        if not userSettings then userSettings = {} end
        local settings = { atis = userSettings }
        self:updateSettings(settings)
        self:updateTransmitter()
        self.comms.atis:SetMsgCallback(HOUND.Sector.generateAtis, self)
        self.comms.atis:enable()
    end

    function HOUND.Sector:disableAtis()
        if self.comms.atis then self.comms.atis:disable() end
    end

    function HOUND.Sector:removeAtis()
        self.settings.atis = nil
        if self.comms.atis then
            self:disableAtis()
            self.comms.atis = nil
        end
    end

    function HOUND.Sector:getAtisFreq()
        if self.comms.atis then
            return self.comms.atis:getFreqs()
        end
        return {}
    end

    function HOUND.Sector:reportEWR(state)
        if self.comms.atis then self.comms.atis:reportEWR(state) end
    end

    function HOUND.Sector:hasAtis() return self.comms.atis ~= nil end

    function HOUND.Sector:isAtisEnabled()
        return self.comms.atis ~= nil and self.comms.atis:isEnabled()
    end

    function HOUND.Sector:enableNotifier(userSettings)
        if not userSettings then userSettings = {} end
        local settings = { notifier = userSettings }
        self:updateSettings(settings)
        self:updateTransmitter()
        self.comms.notifier:enable()
    end

    function HOUND.Sector:disableNotifier()
        if self.comms.notifier then self.comms.notifier:disable() end
    end

    function HOUND.Sector:removeNotifier()
        self.settings.notifier = nil
        if self.comms.notifier then
            self:disableNotifier()
            self.comms.notifier = nil
        end
    end

    function HOUND.Sector:getNotifierFreq()
        if self.comms.notifier then
            return self.comms.notifier:getFreqs()
        end
        return {}
    end

    function HOUND.Sector:hasNotifier()
        return self.comms.notifier ~= nil
    end

    function HOUND.Sector:isNotifierEnabled()
        return self.comms.notifier ~= nil and self.comms.notifier:isEnabled()
    end

    function HOUND.Sector:getNotifier()
        if self:hasNotifier() then
            return self.comms.notifier
        end
        return
    end

    function HOUND.Sector:transmitOnNotifier(msg,priority)
        if not self.comms.notifier or not self.comms.notifier:isEnabled() then return end
        if type(msg) ~= "string" then return end
        if type(priority) ~= "number" then priority = 1 end

        local msgObj = {priority = priority,coalition = self._hSettings:getCoalition()}
        msgObj.tts = msg
        if self.comms.notifier:isEnabled() then
            self.comms.notifier:addMessageObj(msgObj)
        end
    end

    function HOUND.Sector:getContacts()
        local effectiveSectorName = self.name
        if not self:getZone() then
            effectiveSectorName = "default"
        end
        return self._contacts:listAllContactsByRange(effectiveSectorName)
    end

    function HOUND.Sector:countContacts()
        local effectiveSectorName = self.name
        if not self:getZone() then
            effectiveSectorName = "default"
        end
        return self._contacts:countContacts(effectiveSectorName)
    end

    function HOUND.Sector:updateSectorMembership(contact)
        local inSector, threatsSector = HoundUtils.Polygon.threatOnSector(self.settings.zone,contact:getPos(),contact:getMaxWeaponsRange())
        contact:updateSector(self.name, inSector, threatsSector)
        self._contacts:getSite(contact):updateSector()
    end

    function HOUND.Sector:getSites()
        local effectiveSectorName = self.name
        if not self:getZone() then
            effectiveSectorName = "default"
        end
        return self._contacts:listAllSitesByRange(effectiveSectorName)
    end

    function HOUND.Sector:countSites()
        local effectiveSectorName = self.name
        if not self:getZone() then
            effectiveSectorName = "default"
        end
        return self._contacts:countSites(effectiveSectorName)
    end

    function HOUND.Sector.removeRadioMenu(self)
        if self.comms.menu.root ~= nil then
            missionCommands.removeItemForCoalition(self._hSettings:getCoalition(),self.comms.menu.root)
        end
        self.comms.menu = {}
        self.comms.menu.root = nil
        self.comms.enrolled = {}
    end

    function HOUND.Sector:findGrpInPlayerList(grpId,playersList)
        if not playersList or type(playersList) ~= "table" then
            playersList = self.comms.enrolled
        end
        local playersInGrp = {}
        for _,player in pairs(playersList) do
            if player.groupId == grpId then
                table.insert(playersInGrp,player)
            end
        end
        return playersInGrp
    end

    function HOUND.Sector:getSubscribedGroups()
        local subscribedGid = {}
        for _,player in pairs(self.comms.enrolled) do
            local grpId = player.groupId
            if not HOUND.setContainsValue(subscribedGid,grpId) then
                table.insert(subscribedGid,grpId)
            end
        end
        return subscribedGid
    end

    function HOUND.Sector:validateEnrolled()
        if HOUND.Length(self.comms.enrolled) == 0 then return end
        for playerUnitName, player in pairs(self.comms.enrolled) do
            local playerUnit = Unit.getByName(playerUnitName)
            if not HoundUtils.Dcs.isHuman(playerUnit) then
                self.comms.enrolled[player.unitName] = nil
            end
        end
    end

    function HOUND.Sector.checkIn(args,skipAck)
        local gSelf = args["self"]
        local player = args["player"]
        for _,PlayerInGrp in pairs(HOUND.Utils.Dcs.getPlayersInGroup(player.groupName)) do
            gSelf.comms.enrolled[PlayerInGrp.unitName] = PlayerInGrp
        end
        gSelf:populateRadioMenu()
        if not skipAck then
            gSelf:TransmitCheckInAck(player)
        end
    end

    function HOUND.Sector.checkOut(args,skipAck,onlyPlayer)
        local gSelf = args["self"]
        local player = args["player"]
        gSelf.comms.enrolled[player.unitName] = nil

        if not onlyPlayer then
            for _,PlayerInGrp in pairs(HOUND.Utils.Dcs.getPlayersInGroup(player.groupName)) do
                if player.unitName ~= PlayerInGrp.unitName then
                    gSelf.comms.enrolled[PlayerInGrp.unitName] = nil
                end
            end
        end
        gSelf:populateRadioMenu()
        if not skipAck then
            gSelf:TransmitCheckOutAck(player)
        end
    end

    function HOUND.Sector:isNotifiying()
        local controller = self.comms.controller
        local notifier = self.comms.notifier
        if not controller and not notifier then return false end
        if (not controller or not controller:getSettings("alerts") or not controller:isEnabled()) and (not notifier or not notifier:isEnabled())
             then return false end
        return true
    end
    function HOUND.Sector:getTransmissionAnnounce(index)
        local messages = {
            "Attention All Aircraft! This is " .. self.callsign .. ". ",
            "All Aircraft, " .. self.callsign .. ". ",
            "This is " .. self.callsign .. ". "
        }
        local retIndex = l_math.random(1,#messages)
        if type(index) == "number" then
            retIndex = l_math.max(1,l_math.min(#messages,index))
        end
        return messages[retIndex]
    end

    function HOUND.Sector:notifyEmitterDead(contact)
        if not self:isNotifiying() then return end

        local controller = self.comms.controller
        local notifier = self.comms.notifier

        local contactPrimarySector = contact:getPrimarySector()
        if self.name ~= "default" and self.name ~= contactPrimarySector then return end

        if self.name == contactPrimarySector then
            contactPrimarySector = nil
        end

        local announce = self:getTransmissionAnnounce()
        local enrolledGid = self:getSubscribedGroups()

        local msg = {coalition =  self._hSettings:getCoalition(), priority = 3, gid=enrolledGid}
        msg.contactId = contact:getId()
            msg.txt = contact:generateDeathReport(false,contactPrimarySector)
            msg.tts = announce .. contact:generateDeathReport(true,contactPrimarySector)
        if controller and controller:isEnabled() and controller:getSettings("alerts") then
            controller:addMessageObj(msg)
        end
        if notifier and notifier:isEnabled() then
            notifier:addMessageObj(msg)
        end
    end

    function HOUND.Sector:notifyEmitterNew(contact)
        if not self:isNotifiying() then return end

        local controller = self.comms.controller
        local notifier = self.comms.notifier

        local contactPrimarySector = contact:getPrimarySector()
        if self.name ~= "default" and self.name ~= contactPrimarySector then return end

        if self.name == contactPrimarySector then
            contactPrimarySector = nil
        end

        local announce = self:getTransmissionAnnounce()
        local enrolledGid = self:getSubscribedGroups()

        local msg = {coalition = self._hSettings:getCoalition(), priority = 2 , gid=enrolledGid}
        msg.contactId = contact:getId()

            msg.txt = self.callsign .. " Reports " .. contact:generatePopUpReport(false,contactPrimarySector)
            msg.tts = announce .. contact:generatePopUpReport(true,contactPrimarySector)

        if controller and controller:isEnabled() and controller:getSettings("alerts") then
            controller:addMessageObj(msg)
        end

        if notifier and notifier:isEnabled() then
            notifier:addMessageObj(msg)
        end
    end

    function HOUND.Sector:notifySiteIdentified(site)
        if not self:isNotifiying() then return end

        local controller = self.comms.controller
        local notifier = self.comms.notifier

        local sitePrimarySector = site:getPrimarySector()
        if self.name ~= "default" and self.name ~= sitePrimarySector then return end

        if self.name == sitePrimarySector then
            sitePrimarySector = nil
        end

        local announce = self:getTransmissionAnnounce()
        local enrolledGid = self:getSubscribedGroups()

        local msg = {coalition = self._hSettings:getCoalition(), priority = 2 , gid=enrolledGid}

            msg.txt = self.callsign .. " Reports " .. site:generateIdentReport(false,sitePrimarySector)
            msg.tts = announce .. site:generateIdentReport(true,sitePrimarySector)

        if controller and controller:isEnabled() and controller:getSettings("alerts") then
            controller:addMessageObj(msg)
        end

        if notifier and notifier:isEnabled() then
            notifier:addMessageObj(msg)
        end
    end
    function HOUND.Sector:notifySiteNew(site)
        if not self:isNotifiying() then return end

        local controller = self.comms.controller
        local notifier = self.comms.notifier

        local sitePrimarySector = site:getPrimarySector()
        if self.name ~= "default" and self.name ~= sitePrimarySector then return end

        if self.name == sitePrimarySector then
            sitePrimarySector = nil
        end

        local announce = self:getTransmissionAnnounce()
        local enrolledGid = self:getSubscribedGroups()

        local msg = {coalition = self._hSettings:getCoalition(), priority = 2 , gid=enrolledGid}
        msg.contactId = site:getId()
            msg.txt = self.callsign .. " Reports " .. site:generatePopUpReport(false,sitePrimarySector)
            msg.tts = announce .. site:generatePopUpReport(true,sitePrimarySector)
        if controller and controller:isEnabled() and controller:getSettings("alerts") then
            controller:addMessageObj(msg)
        end

        if notifier and notifier:isEnabled() then
            notifier:addMessageObj(msg)
        end

    end
    function HOUND.Sector:notifySiteDead(site,isDead)
        if not self:isNotifiying() then return end

        local controller = self.comms.controller
        local notifier = self.comms.notifier

        local sitePrimarySector = site:getPrimarySector()
        if self.name ~= "default" and self.name ~= sitePrimarySector then return end

        if self.name == sitePrimarySector then
            sitePrimarySector = nil
        end

        local announce = self:getTransmissionAnnounce()
        local enrolledGid = self:getSubscribedGroups()

        local msg = {coalition = self._hSettings:getCoalition(), priority = 3 , gid=enrolledGid}
        msg.contactId = site:getId()
        local body = {}
        if isDead then
            body.txt = site:generateDeathReport(false,sitePrimarySector)
            body.tts = site:generateDeathReport(true,sitePrimarySector)
        else
            body.txt = site:generateAsleepReport(false,sitePrimarySector)
            body.tts = site:generateAsleepReport(true,sitePrimarySector)
        end
        msg.txt = self.callsign .. " Reports " .. body.txt
        msg.tts = announce .. body.tts
        if controller and controller:isEnabled() and controller:getSettings("alerts") then
            controller:addMessageObj(msg)
        end

        if notifier and notifier:isEnabled() then
            notifier:addMessageObj(msg)
        end
    end

function HOUND.Sector:notifySiteLaunching(site)
        if not self._hSettings:getAlertOnLaunch() or not self:isNotifiying() then return end
        local controller = self.comms.controller
        local notifier = self.comms.notifier
        local sitePrimarySector = site:getPrimarySector()
        if self.name ~= "default" and self.name ~= sitePrimarySector then return end

        if self.name == sitePrimarySector then
            sitePrimarySector = nil
        end

        local enrolledGid = self:getSubscribedGroups()

        local msg = {coalition = self._hSettings:getCoalition(), priority = 1 , gid=enrolledGid}
        msg.contactId = site:getId()
        msg.txt = site:generateLaunchAlert(false,sitePrimarySector)
        msg.tts = site:generateLaunchAlert(true,sitePrimarySector)

        if controller and controller:isEnabled() and controller:getSettings("alerts") then
            controller:addMessageObj(msg)
        end

        if notifier and notifier:isEnabled() then
            notifier:addMessageObj(msg)
        end

    end

    function HOUND.Sector:generateAtis(loopData,AtisPreferences)
        local body = ""
        local numberEWR = 0
        local siteCount = self:countSites()
        if siteCount > 0 then
            local sortedSites = self:getSites()
            for _, site in pairs(sortedSites) do
                if site:getPos() ~= nil then
                    if not site.isEWR or
                        (AtisPreferences.reportewr and site.isEWR) then
                        body = body ..
                                    site:generateTtsBrief(
                                        self._hSettings:getNATO()) .. " "
                    end
                    if (not AtisPreferences.reportewr and site.isEWR) then
                        numberEWR = numberEWR + 1
                    end
                end
            end
            if numberEWR > 0 then
                body = body .. numberEWR .. " EWRs are tracked. "
            end
        end

        if body == "" then
            if self._hSettings:getNATO() then
                body = ". EMPTY. "
            else
                body = "No threats had been detected "
            end
        end

        if loopData.body == body then return end
        loopData.body = body

        local reportId
        reportId, loopData.reportIdx =
            HoundUtils.getReportId(loopData.reportIdx)

        local header = self.callsign
        local footer = reportId .. "."

        if self._hSettings:getNATO() then
            header = header .. " Lowdown "
            footer = "Lowdown " .. footer
        else
            header = header .. " SAM information "
            footer = "you have " .. footer
        end
        header = header .. reportId .. " " ..
                                    HoundUtils.TTS.getTtsTime() .. ". "

        local msgObj = {
            coalition = self._hSettings:getCoalition(),
            priority = "loop",
            updateTime = timer.getAbsTime(),
            tts = header .. loopData.body .. footer
        }
        loopData.msg = msgObj
    end

    function HOUND.Sector.TransmitSamReport(args)
        local gSelf = args["self"]
        local contact = gSelf._contacts:getContact(args["contact"],true)
        local requester = args["requester"]
        local coalitionId = gSelf._hSettings:getCoalition()
        local msgObj = {coalition = coalitionId, priority = 1}
        local useDMM = false
        local preferMGRS = false

        if requester == nil then return end
        if contact.isEWR then msgObj.priority = 2 end

        if requester ~= nil then
            msgObj.gid = requester.groupId
            useDMM =  HoundUtils.useDMM(requester.type)
            preferMGRS = HoundUtils.useMGRS(requester.type)
        end

        if gSelf.comms.controller:isEnabled() then
            HOUND.Logger.debug(args["contact"].. ":\n" .. HOUND.Mist.utils.tableShow(contact))

            msgObj.contactId = contact:getId()
            msgObj.tts = contact:generateTtsReport(useDMM,preferMGRS)
            if requester ~= nil then
                msgObj.tts = HoundUtils.getFormationCallsign(requester,gSelf._hSettings:getCallsignOverride()) .. ", " .. gSelf.callsign .. ", " .. msgObj.tts
            end
            if gSelf.comms.controller:getSettings("enableText") == true then
                msgObj.txt = contact:generateTextReport(useDMM)
            end
            HOUND.Logger.debug("msg: \n"..HOUND.Mist.utils.tableShow(msgObj))
            gSelf.comms.controller:addMessageObj(msgObj)
        end
    end

    function HOUND.Sector:TransmitCheckInAck(player)
        if not player then return end
        local msgObj = {priority = 1,coalition = self._hSettings:getCoalition(), gid = player.groupId}
        local msg = HoundUtils.getFormationCallsign(player,self._hSettings:getCallsignOverride()) .. ", " .. self.callsign .. ", Roger. "
        if self:countContacts() > 0 then
            msg = msg .. "Tasking is available."
        else
            msg = msg .. "No known threats."
        end
        msgObj.tts = msg
        msgObj.txt = msg
        if self.comms.controller:isEnabled() then
            self.comms.controller:addMessageObj(msgObj)
        end
    end

    function HOUND.Sector:TransmitCheckOutAck(player)
        if not player then return end
        local msgObj = {priority = 1,coalition = self._hSettings:getCoalition(), gid = player.groupId}
        local msg = HoundUtils.getFormationCallsign(player,self._hSettings:getCallsignOverride()) .. ", " .. self.callsign .. ", copy checking out. "
        msgObj.tts = msg .. "Frequency change approved."
        msgObj.txt = msg
        if self.comms.controller:isEnabled() then
            self.comms.controller:addMessageObj(msgObj)
        end
    end
end

do
    local l_mist = HOUND.Mist

    function HOUND.Sector:getRadioItemsText()
        local menuItems = {
            ['noData'] = "No radars are currently tracked"
        }
        local sites = self:getSites()
        if HOUND.Length(sites) > 0 then
            menuItems.noData = nil
            for _, site in ipairs(sites) do
                if site:getPos() then
                    table.insert(menuItems,site:getRadioItemsText())
                end
            end
        end
        return menuItems
    end

    function HOUND.Sector:createCheckIn()
        for _,player in pairs(self.comms.enrolled) do
            local playerUnit = Unit.getByName(player.unitName)
            if not HOUND.Utils.Dcs.isHuman(playerUnit) then
                    self.comms.enrolled[player.unitName] = nil
            end
        end
        grpMenuDone = {}
        for _,player in pairs(HOUND.DB.HumanUnits.byName[self._hSettings:getCoalition()]) do
            local grpId = player.groupId
            local playerUnit = Unit.getByName(player.unitName)
            if playerUnit and not grpMenuDone[grpId] then
                grpMenuDone[grpId] = true

                if not self.comms.menu[player] then
                    self.comms.menu[player] = self:getMenuObj()
                end

                local grpMenu = self.comms.menu[player]
                local grpPage = self:getMenuPage(grpMenu,grpId,self.comms.menu.root)
                if grpMenu.items.check_in ~= nil then
                    grpMenu.items.check_in = missionCommands.removeItemForGroup(grpId,grpMenu.items.check_in)
                end

                if HOUND.setContainsValue(self.comms.enrolled, player) then
                    grpMenu.items.check_in =
                        missionCommands.addCommandForGroup(grpId,
                                            self.comms.controller:getCallsign() .. " (" ..
                                            self.comms.controller:getFreq() ..") - Check out",
                                            grpPage,HOUND.Sector.checkOut,
                                            {
                                                self = self,
                                                player = player
                                            })
                else
                    grpMenu.items.check_in =
                        missionCommands.addCommandForGroup(grpId,
                                                        self.comms.controller:getCallsign() ..
                                                            " (" ..
                                                            self.comms.controller:getFreq() ..
                                                            ") - Check In",
                                                            grpPage,
                                                        HOUND.Sector.checkIn, {
                            self = self,
                            player = player
                        })
                end
            end
        end
    end

    function HOUND.Sector:populateRadioMenu()
        if self.comms.menu.root ~= nil then
            self.comms.menu.root =
                missionCommands.removeItemForCoalition(self._hSettings:getCoalition(),self.comms.menu.root)
                self.comms.menu.root = nil
        end

        if not self.comms.controller or not self.comms.controller:isEnabled() then return end

        if HOUND.Length(self.comms.menu) > 0 then
            for player,grpMenu in pairs(self.comms.menu) do
                self:removeMenuItems(grpMenu,player.groupId)
            end
        end

        if not self.comms.menu.root then
            self.comms.menu.root =
            missionCommands.addSubMenuForCoalition(self._hSettings:getCoalition(),
                                               self.name,
                                               self._hSettings:getRadioMenu())
        end
        self:validateEnrolled()
        self:createCheckIn()
        local sitesData = self:getRadioItemsText()
        local typesSpotted = {}

        if HOUND.setContains(sitesData.noData) and
            not self.comms.menu.noData then
                self.comms.menu.noData = missionCommands.addCommandForCoalition(self._hSettings:getCoalition(),
                            sitesData.noData,
                            self.comms.menu.root, timer.getAbsTime)
        end

        if not HOUND.setContains(sitesData.noData) then
            if self.comms.menu.noData ~= nil then
                self.comms.menu.noData = missionCommands.removeItemForCoalition(self._hSettings:getCoalition(),
                self.comms.menu.noData)
            end
        end

        local grpMenuDone = {}
        if HOUND.Length(self.comms.enrolled) > 0 then
            if HOUND.Length(sitesData) and not HOUND.setContains(sitesData.noData) then
                for _,siteData in ipairs(sitesData) do
                    if not HOUND.setContainsValue(typesSpotted,siteData.typeAssigned) then
                        table.insert(typesSpotted,siteData.typeAssigned)
                    end
                end
            end
            for _, player in pairs(self.comms.enrolled) do
                local grpId = player.groupId
                local grpMenu = self.comms.menu[player]

                if not grpMenuDone[grpId] and grpMenu ~= nil then
                    grpMenuDone[grpId] = true

                    if not grpMenu.pages then
                        grpMenu.pages = {}
                    end
                    if not grpMenu.items then
                        grpMenu.items = {}
                    end
                    if not grpMenu.objs then
                        grpMenu.objs = {}
                    end

                    for _,typeAssigned in ipairs(typesSpotted) do
                        local newObj = self:getMenuObj()
                        local grpPage = self:getMenuPage(grpMenu,grpId,self.comms.menu.root)
                        grpMenu.items[typeAssigned] = missionCommands.addSubMenuForGroup(grpId,typeAssigned,grpPage)
                        self:getMenuPage(newObj,grpId,grpMenu.items[typeAssigned])
                        grpMenu.objs[typeAssigned] = newObj
                    end

                    for _, siteData in ipairs(sitesData) do
                        local typeMenu = grpMenu.objs[siteData.typeAssigned]
                        self:removeSiteRadioItems(typeMenu,player,siteData)
                        self:addSiteRadioItems(typeMenu,player,siteData)
                    end
                end
            end
        end
    end

    function HOUND.Sector:removeMenuItems(menu,grpId)
        if HOUND.Length(menu.objs) > 0 then
            for objName,obj in pairs (menu.objs) do
                menu.objs[objName]=self:removeMenuItems(obj,grpId)
            end
        end
        if HOUND.Length(menu.items) > 0 then
            for itemName,item in pairs(menu.items) do
                menu.items[itemName]=missionCommands.removeItemForGroup(grpId,item)
            end
        end
        if HOUND.Length(menu.pages) > 0 then
            for idx,page in ipairs(menu.pages) do
                if page ~= nil then
                   menu.pages[idx] = missionCommands.removeItemForGroup(grpId,page)
                end
            end
        end
        return nil
    end

    function HOUND.Sector:getMenuPage(menu,grpId,parent)
        if not menu or type(grpId) ~= "number" then return end

        if not menu.pages then
            menu.pages = {}
        end
        if not menu.items then
            menu.items = {}
        end
        if not menu.objs then
            menu.objs = {}
        end
        if HOUND.Length(menu.pages) == 0 and type(parent) == "table" then
            table.insert(menu.pages,parent)
        end

        local totalItems = (HOUND.Length(menu.items) + #menu.pages)-1
        if (totalItems == HOUND.MENU_PAGE_LENGTH) or (totalItems % #menu.pages) == HOUND.MENU_PAGE_LENGTH then
            table.insert(menu.pages,missionCommands.addSubMenuForGroup(grpId,"More (Page " .. #menu.pages+1 .. ")", menu.pages[#menu.pages]))
        end
        return menu.pages[#menu.pages]
    end

    function HOUND.Sector:getMenuObj()
        return {
            objs = {},
            pages = {},
            items = {}
        }
    end

    function HOUND.Sector:addSiteRadioItems(typeMenu,requester,siteData)
        local playerGid = requester.groupId
        local typePage = self:getMenuPage(typeMenu,playerGid)
        local siteObj = self:getMenuObj()

        typeMenu.items[siteData.dcsName] = missionCommands.addSubMenuForGroup(playerGid, siteData.txt, typePage)
        typeMenu.objs[siteData.dcsName] = siteObj

        for _,emitterData in ipairs(siteData.emitters) do
            local sitePage = self:getMenuPage(typeMenu.objs[siteData.dcsName],playerGid,typeMenu.items[siteData.dcsName])
            siteObj.items[emitterData.dcsName] = missionCommands.addCommandForGroup(playerGid, emitterData.txt, sitePage, self.TransmitSamReport,{self=self,contact=emitterData.dcsName,requester=requester})
        end
    end

    function HOUND.Sector:removeSiteRadioItems(typeMenu,requester,siteData)

        if not self.comms.controller or not self.comms.controller:isEnabled() or not typeMenu or not requester then
            return
        end
        local playerGid = requester.groupId

        local siteObj = typeMenu.objs[siteData.dcsName]
        if HOUND.setContains(siteObj,'items') then
            for emitterName,emitter in (siteObj.items) do
                siteObj.items[emitterName] = missionCommands.removeItemForGroup(playerGid,emitter)
            end
        end

        if HOUND.setContains(typeMenu.items[siteData.dcsName]) then
            typeMenu.items[siteData.dcsName] = missionCommands.removeItemForGroup(playerGid,typeMenu.items[siteData.dcsName] )
        end
    end
end--- Hound Main interface
do
    local HoundUtils = HOUND.Utils
    HoundElint = {}
    HoundElint.__index = HoundElint

    function HoundElint:create(platformName)
        if not platformName then
            HOUND.Logger.error("Failed to initialize Hound instace. Please provide coalition")
            return
        end
        local elint = {}
        setmetatable(elint, HoundElint)
        elint.settings = HOUND.Config.get()
        elint.HoundId = elint.settings:getId()
        elint.contacts = HOUND.ContactManager.get(elint.HoundId)
        elint.elintTaskID = nil
        elint.radioAdminMenu = nil
        elint.coalitionId = nil

        elint.timingCounters = {}

        if platformName ~= nil then
            if type(platformName) == "string" then
                elint:addPlatform(platformName)
            else
                elint:setCoalition(platformName)
            end
        end

        elint.sectors = {
            default = HOUND.Sector.create(elint.HoundId,"default",nil,100)
        }
        elint:defaultEventHandler()

        HOUND.INSTANCES[elint.HoundId] = elint
        return elint
    end

    function HoundElint:destroy()
        self:systemOff(false)
        self:defaultEventHandler(false)

        for name,sector in pairs(self.sectors) do
            self.sectors[name] = sector:destroy()
        end
        self:purgeRadioMenu()
        HOUND.INSTANCES[self.HoundId] = nil
        self.contacts = nil
        self.settings = nil
        return nil
    end

    function HoundElint:getId()
        return self.settings:getId()
    end

    function HoundElint:getCoalition()
        return self.settings:getCoalition()
    end

    function HoundElint:setCoalition(side)
        if side == coalition.side.BLUE or side == coalition.side.RED then
            return self.settings:setCoalition(side)
        end
        return false
    end

    function HoundElint:onScreenDebug(value)
        return self.settings:setOnScreenDebug(value)
    end

    function HoundElint:addPlatform(platformName)
        return self.contacts:addPlatform(platformName)
    end

    function HoundElint:removePlatform(platformName)
        return self.contacts:removePlatform(platformName)
    end

    function HoundElint:countPlatforms()
        return self.contacts:countPlatforms()
    end

    function HoundElint:listPlatforms()
        return self.contacts:listPlatforms()
    end

    function HoundElint:countContacts(sectorName)
        return self.contacts:countContacts(sectorName)
    end

    function HoundElint:countActiveContacts(sectorName)
        local activeContactCount = 0
        local contacts =  self.contacts:getContacts(sectorName)
        for _,contact in pairs(contacts) do
            if contact:isActive() then
                activeContactCount = activeContactCount +1
            end
        end
        return activeContactCount
    end

    function HoundElint:countPreBriefedContacts(sectorName)
        local pbContactCount = 0
        local contacts =  self.contacts:getContacts(sectorName)
        for _,contact in pairs(contacts) do
            if contact:isAccurate() then
                pbContactCount = pbContactCount +1
            end
        end
        return pbContactCount
    end

    function HoundElint:preBriefedContact(DCS_Object_Name,codeName)
        if type(DCS_Object_Name) ~= "string" then return end
        local units = {}
        local obj = Group.getByName(DCS_Object_Name) or Unit.getByName(DCS_Object_Name)
        local grpName = DCS_Object_Name
        if not obj then
            HOUND.Logger.info("Cannot pre-brief " .. DCS_Object_Name .. ": object does not exist.")
            return
        end
        if HoundUtils.Dcs.isGroup(obj) then
            units = obj:getUnits()
        elseif HoundUtils.Dcs.isUnit(obj) then
            table.insert(units,obj)
            grpName = obj:getGroup():getName()
        end

        for _,unit in pairs(units) do
            if unit:getCoalition() ~= self.settings:getCoalition() and unit:isExist() and HOUND.setContains(HOUND.DB.Radars,unit:getTypeName()) then
                self.contacts:setPreBriefedContact(unit)
            end
        end
        if type(codeName) == "string" then
            local site = self.contacts:getSite(grpName,true)
            if site then
                site:setName(codeName)
            end
        end
    end

    function HoundElint:markDeadContact(radarUnit)
        local units={}
        local obj = radarUnit
        if type(radarUnit) == "string" then
            obj = Group.getByName(radarUnit) or Unit.getByName(radarUnit)
        end
        if HoundUtils.Dcs.isGroup(obj) then
            units = obj:getUnits()
            for _,unit in pairs(units) do
                unit = unit:getName()
            end
        elseif HoundUtils.Dcs.isUnit(obj) then
            table.insert(units,obj:getName())
        end
        if not obj then
            if type(radarUnit) == "string" then
                table.insert(units,radarUnit)
            else
                HOUND.Logger.info("Cannot mark as dead: object does not exist.")
                return
            end
        end
        for _,unit in pairs(units) do
            if self.contacts:isContact(unit) then
                self.contacts:setDead(unit)
            end
        end

    end

    function HoundElint:AlertOnLaunch(fireUnit)
        if not self:getAlertOnLaunch() or (not HoundUtils.Dcs.isGroup(fireUnit) and not HoundUtils.Dcs.isUnit(fireUnit)) then return end
        HOUND.Logger.debug("Launch Alert called for " .. fireUnit:getName())

        self.contacts:AlertOnLaunch(fireUnit)
    end

    function HoundElint:countSites(sectorName)
        return self.contacts:countSites(sectorName)
    end

    function HoundElint:addSector(sectorName,sectorSettings,priority)
        if type(sectorName) ~= "string" then return false end
        if string.lower(sectorName) == "default" or string.lower(sectorName) == "all" then
            HOUND.Logger.info(sectorName.. " is a reserved sector name")
            return nil
        end
        priority = priority or 50
        if not self.sectors[sectorName] then
            self.sectors[sectorName] = HOUND.Sector.create(self.settings:getId(),sectorName,sectorSettings,priority)
            if self.settings:getOnScreenDebug() then
                HOUND.Logger.onScreenDebug("Sector " .. sectorName  .. " was added to Hound instance ".. self:getId(),10)
            end
            return self.sectors[sectorName]
        end

        return nil
    end

    function HoundElint:removeSector(sectorName)
        if sectorName == nil then return false end
        self.sectors[sectorName] = self.sectors[sectorName]:destroy()
        if self.settings:getOnScreenDebug() then
            HOUND.Logger.onScreenDebug("Sector " .. sectorName .. " was removed from Hound instance ".. self:getId(),10)
        end
        return true
    end

    function HoundElint:updateSectorSettings(sectorName,sectorSettings,subSettingName)
        if sectorName == nil then sectorName = "default" end
        if not self.sectors[sectorName] then
            env.warn("No sector named ".. sectorName .." was found.")
            return false
        end
        if sectorSettings == nil or type(sectorSettings) ~= "table" then return false end
        local sector = self.sectors[sectorName]
        if subSettingName ~= nil and type(subSettingName) == "string" then
            local subSetting = string.lower(subSettingName)
            if subSetting == "controller" or subSetting == "atis" or subSetting == "notifier" then
                local generatedSettings = {}
                generatedSettings[subSetting] = sectorSettings
                sector:updateSettings(generatedSettings)
                return true
            end
        end
        sector:updateSettings(sectorSettings)
        return true
    end

    function HoundElint:listSectors(element)
        local sectors = {}
        for name,sector in pairs(self.sectors) do
            local addToList = true
            if element then
                if string.lower(element) == "controller" then
                    addToList=sector:hasController()
                end
                if string.lower(element) == "atis" then
                    addToList=sector:hasAtis()
                end
                if string.lower(element) == "notifier" then
                    addToList=sector:hasNotifier()
                end
                if string.lower(element) == "zone" then
                    addToList=sector:hasZone()
                end
            end

            if addToList then
                table.insert(sectors,name)
            end
        end
        return sectors
    end

    function HoundElint:getSectors(element)
        local sectors = {}
        for _,sector in pairs(self.sectors) do
            local addToList = true
            if element then
                if string.lower(element) == "controller" then
                    addToList=sector:hasController()
                end
                if string.lower(element) == "atis" then
                    addToList=sector:hasAtis()
                end
                if string.lower(element) == "notifier" then
                    addToList=sector:hasNotifier()
                end
                if string.lower(element) == "zone" then
                    addToList=sector:hasZone()
                end
            end

            if addToList then
                table.insert(sectors,sector)
            end
        end
        return sectors
    end

    function HoundElint:countSectors(element)
        return HOUND.Length(self:listSectors(element))
    end

    function HoundElint:getSector(sectorName)
        if HOUND.setContains(self.sectors,sectorName) then
            return self.sectors[sectorName]
        end
    end

    function HoundElint:enableController(sectorName,settings)
        if type(sectorName) == "table" and settings == nil then
            settings  = sectorName
            sectorName = "default"
        end
        if sectorName == nil then sectorName = "default" end
        if self.sectors[sectorName] ~= nil then
            self.sectors[sectorName]:enableController(settings)
            return
        end
        if string.lower(sectorName) == "all" and settings == nil then
            for _,sector in pairs(self.sectors) do
                sector:enableController()
            end
        end

    end

    function HoundElint:disableController(sectorName)
        if sectorName == nil then
            sectorName = "default"
        end
        if self.sectors[sectorName] ~= nil then
            self.sectors[sectorName]:disableController()
        end
        if sectorName:lower() == "all" then
            for _,sector in pairs(self.sectors) do
                sector:disableController()
            end
        end
    end

    function HoundElint:removeController(sectorName)
        if sectorName == nil then
            sectorName = "default"
        end
        if sectorName:lower() == "all" then
            for _,sector in pairs(self.sectors) do
                sector:removeController()
            end
        elseif self.sectors[sectorName] ~= nil then
            self.sectors[sectorName]:removeController()
        end
    end

    function HoundElint:configureController(sectorName,settings)
        if sectorName == nil and settings == nil then return end
        if sectorName == nil and type(settings) == "table" then
            sectorName = "default"
        end
        if type(sectorName) =="table" and settings == nil then
            settings = sectorName
            sectorName = "default"
        end
        local controllerSettings = { controller = settings}
        if self.sectors[sectorName] == nil then
            self:addSector(sectorName,controllerSettings)
        elseif self.sectors[sectorName] then
            self.sectors[sectorName]:updateSettings(controllerSettings)
        end
    end

    function HoundElint:getControllerFreq(sectorName)
        sectorName = sectorName or "default"
        return self.sectors[sectorName]:getControllerFreq() or {}
    end

    function HoundElint:getControllerState(sectorName)
        sectorName = sectorName or "default"

        if self.sectors[sectorName] then
            return (self.sectors[sectorName]:isControllerEnabled())
        end
        return false
    end

    function HoundElint:transmitOnController(sectorName,msg,priority)
        if not sectorName or not msg then return end
        if self.sectors[sectorName] then
            self.sectors[sectorName]:transmitOnController(msg,priority)
            return
        end
        if sectorName == "all" then
            for _,sector in pairs(self.sectors) do
                sector:transmitOnController(msg,priority)
            end
        end
    end

    function HoundElint:enableAtis(sectorName,settings)
        if type(sectorName) == "table" and settings == nil then
            settings  = sectorName
            sectorName = "default"
        end
        if sectorName == nil then sectorName = "default" end
        if string.lower(sectorName) == "all" then
            for _,sector in pairs(self.sectors) do
                sector:enableAtis()
            end
            return
        end
        if self.sectors[sectorName] ~= nil then
            self.sectors[sectorName]:enableAtis(settings)
        end
    end

    function HoundElint:disableAtis(sectorName)
        if sectorName == nil then
            sectorName = "default"
        end
        if self.sectors[sectorName] ~= nil then
            self.sectors[sectorName]:disableAtis()
            return
        end
        if sectorName == "all" then
            for _,sector in pairs(self.sectors) do
                sector:disableAtis()
            end
        end
    end

    function HoundElint:removeAtis(sectorName)
        if sectorName == nil then
            sectorName = "default"
        end
        if sectorName == "all" then
            for _,sector in pairs(self.sectors) do
                sector:removeAtis()
            end
        elseif self.sectors[sectorName] ~= nil then
            self.sectors[sectorName]:removeAtis()
        end
    end

    function HoundElint:configureAtis(sectorName,settings)
        if sectorName == nil and settings == nil then return end
        if sectorName == nil and type(settings) == "table" then
            sectorName = "default"
        end
        if type(sectorName) =="table" and settings == nil then
            settings = sectorName
            sectorName = "default"
        end
        local userSettings = { atis = settings}
        if self.sectors[sectorName] == nil then
            self:addSector(sectorName,userSettings)
        elseif self.sectors[sectorName] then
            self.sectors[sectorName]:updateSettings(userSettings)
        end
    end

    function HoundElint:getAtisFreq(sectorName)
        sectorName = sectorName or "default"
        return self.sectors[sectorName]:getAtisFreq() or {}
    end

    function HoundElint:reportEWR(name,state)
        if type(name) == "boolean" then
            state = name
            name = "default"
        end
        if self.sectors[name] then
            self.sectors[name]:reportEWR(state)
            return
        end
        if name == "all" then
            for _,sector in pairs(self.sectors) do
                sector:reportEWR(state)
            end
        end
    end

    function HoundElint:getAtisState(sectorName)
        sectorName = sectorName or "default"
        if self.sectors[sectorName] then
            return (self.sectors[sectorName]:isAtisEnabled())
        end
        return false
    end

    function HoundElint:enableNotifier(sectorName,settings)
        if type(sectorName) == "table" and settings == nil then
            settings  = sectorName
            sectorName = "default"
        end
        if sectorName == nil then sectorName = "default" end
        if self.sectors[sectorName] ~= nil then
            self.sectors[sectorName]:enableNotifier(settings)
        end
    end

    function HoundElint:disableNotifier(sectorName)
        if sectorName == nil then
            sectorName = "default"
        end
        if sectorName == "all" then
            for _,sector in pairs(self.sectors) do
                sector:disableNotifier()
            end
            return
        end
        if self.sectors[sectorName] ~= nil then
            self.sectors[sectorName]:disableNotifier()
        end
    end

    function HoundElint:removeNotifier(sectorName)
        if sectorName == nil then
            sectorName = "default"
        end
        if sectorName == "all" then
            for _,sector in pairs(self.sectors) do
                sector:removeNotifier()
            end
            return
        end
        if self.sectors[sectorName] ~= nil then
            self.sectors[sectorName]:removeNotifier()
        end
    end

    function HoundElint:configureNotifier(sectorName,settings)
        if sectorName == nil and settings == nil then return end
        if sectorName == nil and type(settings) == "table" then
            sectorName = "default"
        end
        if type(sectorName) =="table" and settings == nil then
            settings = sectorName
            sectorName = "default"
        end
        local notifierSettings = { notifier = settings}
        if self.sectors[sectorName] == nil then
            self:addSector(sectorName,notifierSettings)
        elseif self.sectors[sectorName] then
            self.sectors[sectorName]:updateSettings(notifierSettings)
        end
    end

    function HoundElint:getNotifierFreq(sectorName)
        sectorName = sectorName or "default"
        return self.sectors[sectorName]:getNotifierFreq() or {}
    end

    function HoundElint:getNotifierState(sectorName)
        sectorName = sectorName or "default"
        if self.sectors[sectorName] then
            return (self.sectors[sectorName]:isNotifierEnabled())
        end
        return false
    end

    function HoundElint:transmitOnNotifier(sectorName,msg,priority)
        if not sectorName or not msg then return end
        if self.sectors[sectorName] then
            self.sectors[sectorName]:transmitOnNotifier(msg,priority)
            return
        end
        if sectorName == "all" then
            for _,sector in pairs(self.sectors) do
                sector:transmitOnNotifier(msg,priority)
            end
        end
    end

    function HoundElint:enableText(sectorName)
        if sectorName == nil or type(sectorName) ~= "string" then
            sectorName = "default"
        end
        if self.sectors[sectorName] then
            self.sectors[sectorName]:enableText()
            return
        end
        if string.lower(sectorName) == "all" then
            for _,sector in pairs(self.sectors) do
                sector:enableText()
            end
        end

    end

    function HoundElint:disableText(sectorName)
        if sectorName == nil or type(sectorName) ~= "string" then
            sectorName = "default"
        end
        if self.sectors[sectorName] then
            self.sectors[sectorName]:disableText()
            return
        end
        if string.lower(sectorName) == "all" then
            for _,sector in pairs(self.sectors) do
                sector:disableText()
            end
        end
    end

    function HoundElint:enableTTS(sectorName)
        if sectorName == nil or type(sectorName) ~= "string" then
            sectorName = "default"
        end
        if self.sectors[sectorName] then
            self.sectors[sectorName]:enableTTS()
            return
        end
        if string.lower(sectorName) == "all" then
            for _,sector in pairs(self.sectors) do
                sector:enableTTS()
            end
        end
    end

    function HoundElint:disableTTS(sectorName)
        if sectorName == nil or type(sectorName) ~= "string" then
            sectorName = "default"
        end
        if self.sectors[sectorName] then
            self.sectors[sectorName]:disableTTS()
            return
        end
        if string.lower(sectorName) == "all" then
            for _,sector in pairs(self.sectors) do
                sector:disableTTS()
            end
        end
    end

    function HoundElint:enableAlerts(sectorName)
        if sectorName == nil or type(sectorName) ~= "string" then
            sectorName = "default"
        end
        if self.sectors[sectorName] then
            self.sectors[sectorName]:enableAlerts()
            return
        end
        if string.lower(sectorName) == "all" then
            for _,sector in pairs(self.sectors) do
                sector:enableAlerts()
            end
        end

    end

    function HoundElint:disableAlerts(sectorName)
        if sectorName == nil or type(sectorName) ~= "string" then
            sectorName = "default"
        end
        if self.sectors[sectorName] then
            self.sectors[sectorName]:disableAlerts()
            return
        end
        if string.lower(sectorName) == "all" then
            for _,sector in pairs(self.sectors) do
                sector:disableAlerts()
            end
        end
    end

    function HoundElint:setCallsign(sectorName,sectorCallsign)
        if not sectorName then return false end
        local NATO = self.settings:getUseNATOCallsigns()
        if sectorCallsign == "NATO" then
            sectorCallsign = true
        end
        if type(sectorCallsign) == "boolean" then
            NATO = sectorCallsign
            sectorCallsign = nil
        end
        if self.sectors[sectorName] then
            self.sectors[sectorName]:setCallsign(sectorCallsign,NATO)
            return true
        end
        return false
    end

    function HoundElint:getCallsign(sectorName)
        if not sectorName then return "" end
        if self.sectors[sectorName] then
            return self.sectors[sectorName]:getCallsign()
        end
        return ""
    end

    function HoundElint:setTransmitter(sectorName,transmitter)
        if not sectorName and not transmitter then return end
        if sectorName and not transmitter then
            transmitter = sectorName
            sectorName = "default"
        end
        if sectorName == nil then sectorName = "default" end
        if string.lower(sectorName) == "all" then
            for _,sector in pairs(self.sectors) do
                sector:setTransmitter(transmitter)
            end
        end
        if self.sectors[sectorName] then
            self.sectors[sectorName]:setTransmitter(transmitter)
        end
    end

    function HoundElint:removeTransmitter(sectorName)
        if sectorName == nil then sectorName = "default" end
        if string.lower(sectorName) == "all" then
            for _,sector in pairs(self.sectors) do
                sector:removeTransmitter()
            end
        end
        if self.sectors[sectorName] then
            self.sectors[sectorName]:removeTransmitter()
        end
    end

    function HoundElint:getZone(sectorName)
        sectorName = sectorName or "default"
        if self.sectors[sectorName] then
            return self.sectors[sectorName]:getZone()
        end
    end

    function HoundElint:setZone(sectorName,zoneCandidate)
        if type(sectorName) ~= "string" then return end
        if type(zoneCandidate) ~= "string" and zoneCandidate ~= nil then return end
        if self.sectors[sectorName] then
            self.sectors[sectorName]:setZone(zoneCandidate)
        end
        self:updateSectorMembership()
    end

    function HoundElint:removeZone(sectorName)
        if self.sectors[sectorName] then
            self.sectors[sectorName]:removeZone()
        end
        self:updateSectorMembership()
    end

    function HoundElint:updateSectorMembership()
        local sectors = self:getSectors()
        table.sort(sectors,HoundUtils.Sort.sectorsByPriorityLowFirst)
        for _,contact in ipairs(self.contacts:listAllContacts()) do
            for _,sector in pairs(sectors) do
                sector:updateSectorMembership(contact)
            end
        end
        for _,site in ipairs(self.contacts:listAllSites()) do
            site:updateSector()
        end
    end

    function HoundElint:enableMarkers(markerType)
        if markerType and HOUND.setContainsValue(HOUND.MARKER,markerType) then
            self:setMarkerType(markerType)
        end
        return self.settings:setUseMarkers(true)
    end

    function HoundElint:disableMarkers()
        return self.settings:setUseMarkers(false)
    end

    function HoundElint:enableSiteMarkers()
        return self.settings:setMarkSites(true)
    end

    function HoundElint:disableSiteMarkers()
        return self.settings:setMarkSites(false)
    end

    function HoundElint:setMarkerType(markerType)
        if markerType and HOUND.setContainsValue(HOUND.MARKER,markerType) then
            return self.settings:setMarkerType(markerType)
        end
        return false
    end

    function HoundElint:setTimerInterval(setIntervalName,setValue)
        if self.settings and HOUND.setContains(self.settings.intervals,string.lower(setIntervalName)) then
            return self.settings:setInterval(setIntervalName,setValue)
        end
        return false
    end

    function HoundElint:enablePlatformPosErrors()
        return self.settings:setPosErr(true)
    end

    function HoundElint:disablePlatformPosErrors()
        return self.settings:setPosErr(false)
    end

    function HoundElint:getCallsignOverride()
        return self.settings:getCallsignOverride()
    end

    function HoundElint:setCallsignOverride(overrides)
        return self.settings:setCallsignOverride(overrides)
    end

    function HoundElint:getBDA()
        return self.settings:getBDA()
    end

    function HoundElint:enableBDA()
        return self.settings:setBDA(true)
    end

    function HoundElint:disableBDA()
        return self.settings:setBDA(false)
    end

    function HoundElint:getNATO()
        return self.settings:getNATO()
    end

    function HoundElint:enableNATO()
        return self.settings:setNATO(true)
    end

    function HoundElint:disableNATO()
        return self.settings:setNATO(false)
    end

    function HoundElint:getAlertOnLaunch()
        return self.settings:getAlertOnLaunch()
    end

    function HoundElint:setAlertOnLaunch(value)
        return self.settings:setAlertOnLaunch(value)
    end

    function HoundElint:useNATOCallsignes(value)
        if type(value) ~= "boolean" then return false end
        return self.settings:setUseNATOCallsigns(value)
    end

    function HoundElint:setAtisUpdateInterval(value)
        return self.settings:setAtisUpdateInterval(value)
    end

    function HoundElint:setRadioMenuParent(parent)
        local retval = self.settings:setRadioMenuParent(parent)
        if retval == true and self:isRunning() then
            self:populateRadioMenu()
        end
        return retval or false
    end

    function HoundElint.runCycle(self)
        local runTime = timer.getAbsTime()
        local nextRun = timer.getTime() + HOUND.Gaussian(self.settings.intervals.scan,self.settings.intervals.scan/10)
        if self.settings:getCoalition() == nil then return nextRun end
        if not self.contacts then return nextRun end

        self.contacts:platformRefresh()
        self.contacts:Sniff()

        if self.contacts:countContacts() > 0 then
            local doProcess = true
            local doMenus = false
            local doMarkers = false
            if self.timingCounters.lastProcess then
                doProcess = ((HoundUtils.absTimeDelta(self.timingCounters.lastProcess,runTime)/self.settings.intervals.process) > 0.99)
            end
            if self.timingCounters.lastMenus then
                doMenus = ((HoundUtils.absTimeDelta(self.timingCounters.lastMenus,runTime)/self.settings.intervals.menus) > 0.99)
            end
            if self.timingCounters.lastMarkers then
                doMarkers = ((HoundUtils.absTimeDelta(self.timingCounters.lastMarkers,runTime)/self.settings.intervals.markers) > 0.99)
            end

            if doProcess then
                self.contacts:Process()
                self:updateSectorMembership()

                self.timingCounters.lastProcess = runTime
                if not self.timingCounters.lastMarkers then
                    self.timingCounters.lastMarkers = runTime
                end
                if not self.timingCounters.lastMenus then
                    self.timingCounters.lastMenus = runTime
                end
            end

            if doMenus then
                self:populateRadioMenu()
                self.timingCounters.lastMenus = runTime
            end

            if doMarkers then
                self.contacts:UpdateMarkers()
                self.timingCounters.lastMarkers = runTime
            end
        end
        if self.settings:getOnScreenDebug() then
            HOUND.Logger.onScreenDebug(self:printDebugging(),self.settings.intervals.scan*0.75)
        end
        return nextRun
    end

    function HoundElint:purgeRadioMenu()
        for _,sector in pairs(self:getSectors()) do
            sector:removeRadioMenu()
        end
        self.settings:removeRadioMenu()
    end

    function HoundElint:populateRadioMenu()
        if not self:isRunning() or not self.contacts or self.contacts:countContacts() == 0 or self.settings:getCoalition() == nil then
            return
        end
        HOUND.DB.updateHumanDb(self.settings:getCoalition())
        local sectors = self:getSectors()
        table.sort(sectors,HoundUtils.Sort.sectorsByPriorityLowLast)
        for _,sector in pairs(sectors) do
            sector:populateRadioMenu()
        end
    end

    function HoundElint.updateSystemState(params)
        local state = params.state
        local self = params.self
        if state == true then
            self:systemOn()
        elseif state == false then
            self:systemOff()
        end
    end

    function HoundElint:systemOn(notify)
        if self.settings:getCoalition() == nil then
            HOUND.Logger.warn("failed to start. no coalition found.")
            return false
        end
        self:systemOff(false)

        self.elintTaskID = timer.scheduleFunction(self.runCycle, self, timer.getTime() + self.settings.intervals.scan)
        if notify == nil or notify then
            trigger.action.outTextForCoalition(self.settings:getCoalition(),
                                           "Hound ELINT system is now Operating", 10)
        end
        env.info("Hound is now on")
        HOUND.EventHandler.publishEvent({
            id = HOUND.EVENTS.HOUND_ENABLED,
            houndId = self.settings:getId(),
            coalition = self.settings:getCoalition()
        })
        return true
    end

    function HoundElint:systemOff(notify)
        if self.elintTaskID ~= nil then
            timer.removeFunction(self.elintTaskID)
        end
        self:purgeRadioMenu()
        if notify == nil or notify then
            trigger.action.outTextForCoalition(self.settings:getCoalition(),
                                           "Hound ELINT system is now Offline", 10)
        end
        env.info("Hound is now off")
        HOUND.EventHandler.publishEvent({
            id = HOUND.EVENTS.HOUND_DISABLED,
            houndId = self.settings:getId(),
            coalition = self.settings:getCoalition()
        })
        return true
    end

    function HoundElint:isRunning()
        return (self.elintTaskID ~= nil)
    end

    function HoundElint:getContacts()
        local contacts = {
            ewr = { contacts = {} },
            sam = { contacts = {} }
            }
        for _,emitter in pairs(self.contacts:listAllContacts()) do
            local contact = emitter:export()
            if contact ~= nil then
                if emitter.isEWR then
                    table.insert(contacts.ewr.contacts,contact)
                else
                    table.insert(contacts.sam.contacts,contact)
                end
            end
        end
        contacts.ewr.count = #contacts.ewr.contacts or 0
        contacts.sam.count = #contacts.sam.contacts or 0
        return contacts
    end

    function HoundElint:getSites()
        local contacts = {
            ewr = { sites = {} },
            sam = { sites = {} }
        }
        for _,site in pairs(self.contacts:listAllSites()) do
            local contact = site:export()
            if contact ~= nil then
                if site.isEWR then
                    table.insert(contacts.ewr.sites,contact)
                else
                    table.insert(contacts.sam.sites,contact)
                end
            end
        end
        contacts.ewr.count = #contacts.ewr.sites or 0
        contacts.sam.count = #contacts.sam.sites or 0
        return contacts
    end

    function HoundElint:dumpIntelBrief(filename)
        if lfs == nil or io == nil then
            HOUND.Logger.info("cannot write CSV. please desanitize lfs and io")
            return
        end
        if not filename then
            filename = string.format("hound_contacts_%d.csv",self:getId())
        end
        local currentGameTime = HoundUtils.Text.getTime()
        local csvFile = io.open(lfs.writedir() .. filename, "w+")
        csvFile:write("SiteId,SiteNatoDesignation,TrackId,RadarType,State,Bullseye,Latitude,Longitude,MGRS,Accuracy,lastSeen,DcsType,DcsUnit,DcsGroup,ReportGenerated\n")
        csvFile:flush()
        for _,site in pairs(self.contacts:listAllSitesByRange()) do
            local siteItems = site:generateIntelBrief()
            if #siteItems > 0 then
                for _,item in ipairs(siteItems) do
                    csvFile:write(item .. "," .. currentGameTime .."\n")
                    csvFile:flush()
                end
            end
        end
        csvFile:close()
    end

    function HoundElint:printDebugging()
        local debugMsg = "Hound instace " .. self:getId() .. " (".. HoundUtils.getCoalitionString(self:getCoalition()) .. ")\n"
        debugMsg = debugMsg .. "-----------------------------\n"
        debugMsg = debugMsg .. "Platforms: " .. self:countPlatforms() .. " | sectors: " .. self:countSectors()
        debugMsg = debugMsg .. " (Z:"..self:countSectors("zone").." ,C:"..self:countSectors("controller").." ,A: " .. self:countSectors("atis") .. " ,N:"..self:countSectors("notifier") ..") | "
        debugMsg = debugMsg .. "Sites: " .. self:countSites() .. " | Contacts: ".. self:countContacts() .. " (A:" .. self:countActiveContacts() .. " ,PB:" .. self:countPreBriefedContacts() .. ")"
        return debugMsg
    end
end

do

    local HoundUtils = HOUND.Utils

    function HoundElint:onHoundEvent(houndEvent)
        return nil
    end

    function HoundElint:onHoundInternalEvent(houndEvent)
        if houndEvent.houndId ~= self.settings:getId() then
            return
        end
        if houndEvent.id == HOUND.EVENTS.HOUND_DISABLED then return end

        local sectors = self:getSectors()
        table.sort(sectors,HoundUtils.Sort.sectorsByPriorityLowFirst)

        if houndEvent.id == HOUND.EVENTS.RADAR_DETECTED then
            for _,sector in pairs(sectors) do
                sector:updateSectorMembership(houndEvent.initiator)
            end
        end
        if self:isRunning() then

            for _,sector in pairs(sectors) do
                if houndEvent.id == HOUND.EVENTS.RADAR_DESTROYED then
                    sector:notifyEmitterDead(houndEvent.initiator)
                end
                if houndEvent.id == HOUND.EVENTS.SITE_CREATED then
                    if not houndEvent.initiator.isEWR then
                        sector:notifySiteNew(houndEvent.initiator)
                    end
                end
                if houndEvent.id == HOUND.EVENTS.SITE_CLASSIFIED then
                    if not houndEvent.initiator.isEWR then
                        sector:notifySiteIdentified(houndEvent.initiator)
                    end
                end
                if houndEvent.id == HOUND.EVENTS.SITE_REMOVED or houndEvent.id == HOUND.EVENTS.SITE_ASLEEP then
                    sector:notifySiteDead(houndEvent.initiator,(houndEvent.id == HOUND.EVENTS.SITE_REMOVED))
                end
                if houndEvent.id == HOUND.EVENTS.SITE_LAUNCH then
                    sector:notifySiteLaunching(houndEvent.initiator)
                end
            end

            if houndEvent.id == HOUND.EVENTS.SITE_CREATED or houndEvent.id == HOUND.EVENTS.SITE_CLASSIFIED then
                self:populateRadioMenu()
                if self.settings:getMarkSites() then
                    houndEvent.initiator:updateMarker(HOUND.MARKER.NONE)
                end
            end
            if houndEvent.id == HOUND.EVENTS.RADAR_DETECTED then
                if self.settings:getUseMarkers() then
                    houndEvent.initiator:updateMarker(self.settings:getMarkerType())
                end
            end
            if not self.settings:getBDA() then return end
            if houndEvent.id == HOUND.EVENTS.SITE_REMOVED then
                houndEvent.initiator:destroy()
                self.contacts:removeSite(houndEvent.initiator)
                self:populateRadioMenu()
            end
            if houndEvent.id == HOUND.EVENTS.RADAR_DESTROYED then
                self.contacts:removeContact(houndEvent.initiator)
                self:populateRadioMenu()
            end
        end
    end

    function HoundElint:onEvent(DcsEvent)
        if not HoundUtils.Dcs.isUnit(DcsEvent.initiator) then return end

        if DcsEvent.id == world.event.S_EVENT_UNIT_LOST
            and DcsEvent.initiator:getCoalition() ~= self.settings:getCoalition()
            and self:getBDA()
            then
                return self:markDeadContact(DcsEvent.initiator)
        end

        if not self:isRunning() then return end

        if (DcsEvent.id == world.event.S_EVENT_BIRTH)
            and DcsEvent.initiator:getCoalition() == self.settings:getCoalition()
            and HoundUtils.Dcs.isHuman(DcsEvent.initiator)
        then
            local _,catEx = DcsEvent.initiator:getCategory()
            if not HOUND.setContainsValue({Unit.Category.AIRPLANE,Unit.Category.HELICOPTER},catEx) then return end
            return self:populateRadioMenu()
        end

        if (DcsEvent.id == world.event.S_EVENT_PLAYER_LEAVE_UNIT
            or DcsEvent.id == world.event.S_EVENT_PILOT_DEAD
            or DcsEvent.id == world.event.S_EVENT_EJECTION)
            and DcsEvent.initiator:getCoalition() == self.settings:getCoalition()
            and HoundUtils.Dcs.isHuman(DcsEvent.initiator)
        then
            local _,catEx = DcsEvent.initiator:getCategory()
            if not HOUND.setContainsValue({Unit.Category.AIRPLANE,Unit.Category.HELICOPTER},catEx) then return end
            return self:populateRadioMenu()
        end

        if DcsEvent.id == world.event.S_EVENT_SHOT
            and DcsEvent.initiator:getCoalition() ~= self.settings:getCoalition()
            and DcsEvent.initiator:hasAttribute("Air Defence")
            and DcsEvent.initiator:getCategory() == Object.Category.UNIT
        then
            local _,catEx = DcsEvent.initiator:getCategory()
            if not HOUND.setContainsValue({Unit.Category.GROUND_UNIT,Unit.Category.SHIP},catEx) then return end
            local grp = DcsEvent.initiator:getGroup()
            if HoundUtils.Dcs.isGroup(grp) then
                self.contacts:Sniff(grp:getName())
                if DcsEvent.weapon:getDesc().category ~= Weapon.Category.MISSILE then return end
                local tgtPos = nil
                local wpnTgt = DcsEvent.weapon:getTarget()
                if HoundUtils.Dcs.isUnit(wpnTgt) then
                  tgtPos = wpnTgt:getPoint()
                end
                if HoundUtils.Dcs.isPoint(tgtPos) then
                    HoundUtils.Geo.setPointHeight(tgtPos)
                end
                self.contacts:ensureSitePrimaryHasPos(grp:getName(),tgtPos) -- pass target position, if no position available, it will alert on target position
                self:AlertOnLaunch(grp)
            end
        end

        if DcsEvent.id == world.event.S_EVENT_WEAPON_ADD
            and HOUND.AUTO_ADD_PLATFORM_BY_PAYLOAD
            and HoundUtils.Dcs.isUnit(DcsEvent.initiator)
            and DcsEvent.initiator:getCoalition() == self.settings:getCoalition()
        then
            local unit = DcsEvent.initiator
            if not HOUND.DB.isValidPlatform(unit,DcsEvent.weapon_name) then return end
            self:addPlatform(unit:getName())
        end
    end

    function HoundElint:defaultEventHandler(remove)
        if remove == false then
            HOUND.EventHandler.removeInternalEventHandler(self)
            world.removeEventHandler(self)
            return
        end
        HOUND.EventHandler.addInternalEventHandler(self)
        world.addEventHandler(self)
    end
end
do
    trigger.action.outText("Hound ELINT ("..HOUND.VERSION..") is loaded.", 15)
    env.info("[Hound] - finished loading (".. HOUND.VERSION..")")
end
-- Hound version 0.5.0 - Compiled on 2026-02-24 22:19